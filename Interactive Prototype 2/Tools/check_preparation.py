"""Verify the IP2 preparation bundle without claiming studio or headset completion."""
from pathlib import Path
import csv
import hashlib
import json
import unittest
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / 'Interactive Prototype 2'


class IP2PreparationChecks(unittest.TestCase):
    def test_required_documents_exist(self):
        for relative in ('README.md', 'Testing/Testing Plan',
                         'Testing/Participant Record', 'Design Journal/Design Journal'):
            if relative.endswith('.md'):
                self.assertTrue((PACK / relative).is_file())
            else:
                for extension in ('.md', '.docx', '.pdf'):
                    self.assertTrue((PACK / (relative + extension)).is_file(), relative + extension)

    def test_results_have_not_been_filled_in_advance(self):
        with (PACK / 'Results/Session Results.csv').open(newline='') as file:
            rows = list(csv.DictReader(file))
        self.assertEqual(len(rows), 40)
        for row in rows:
            for field in ('result', 'elapsed_seconds', 'attempts', 'prompt_count', 'observation'):
                self.assertEqual(row[field], '')
        with (PACK / 'Results/Post-Test Ratings.csv').open(newline='') as file:
            for row in csv.DictReader(file):
                self.assertEqual(row['rating'], '')

    def test_source_archive_contains_openable_unity_project(self):
        with ZipFile(PACK / 'Source Code/Whiteboard Unity Project.zip') as archive:
            names = set(archive.namelist())
            for relative in ('Packages/manifest.json', 'ProjectSettings/ProjectVersion.txt',
                             'Assets/XRStudyWhiteboard/Scenes/XRStudyClassroom.unity'):
                self.assertIn('Whiteboard/' + relative, names)
            self.assertFalse(any('/Library/' in name or '/Temp/' in name for name in names))

    def test_apk_is_packaged(self):
        with ZipFile(PACK / 'Builds/XRStudyWhiteboard_Quest2_v1.3.apk') as apk:
            self.assertIn('AndroidManifest.xml', apk.namelist())
            self.assertTrue(any(n.startswith('lib/arm64-v8a/') for n in apk.namelist()))

    def test_unfinished_requirements_are_explicit(self):
        text = (PACK / 'README.md').read_text()
        for phrase in ('PENDING', 'working-prototype video', 'mandatory studio',
                       'course uploads', 'pre-session'):
            self.assertIn(phrase, text)
        self.assertTrue((PACK / 'Media/Recording Guide.md').is_file())

    def test_provided_video_inventory(self):
        manifest = json.loads((PACK / 'Media/Video Manifest.json').read_text())
        self.assertEqual(len(manifest['files']), 2)
        for record in manifest['files']:
            video = PACK / 'Media' / record['filename']
            self.assertEqual(video.stat().st_size, record['bytes'])
            self.assertGreater(record['duration_seconds'], 330)
            self.assertEqual(record['video_track_count'], 1)
            self.assertEqual(record['audio_track_count'], 1)

    def test_code_guide_matches_all_custom_source_files(self):
        index = json.loads((PACK / 'Code Study/Code Index.json').read_text())
        guide = (PACK / 'Code Study/Code Study Guide.md').read_text()
        source = ROOT / 'Whiteboard/Assets/XRStudyWhiteboard'
        actual = set((source / 'Scripts').rglob('*.cs')) | set((source / 'Editor').glob('*.cs'))
        self.assertEqual(len(index['scripts']), len(actual))
        for record in index['scripts']:
            path = ROOT / record['file']
            self.assertIn(path, actual)
            self.assertIn(path.name, guide)
            self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(), record['sha256'])
        for extension in ('.md', '.docx', '.pdf'):
            self.assertTrue((PACK / 'Code Study' / ('Code Study Guide' + extension)).is_file())


if __name__ == '__main__':
    unittest.main(verbosity=2)
