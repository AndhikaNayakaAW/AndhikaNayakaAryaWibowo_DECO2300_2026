"""Build the code study handout and an index of the actual custom C# source."""
from pathlib import Path
import hashlib
import importlib.util
import json
import re
import subprocess
from urllib.parse import quote

ROOT = Path(__file__).resolve().parents[3]
PACK = ROOT / 'Working Files' / 'Interactive Prototype 2'
CUSTOM = ROOT / 'Whiteboard/Assets/XRStudyWhiteboard'


def build_index():
    commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT).decode().strip()
    files = sorted(list((CUSTOM / 'Scripts').rglob('*.cs')) + list((CUSTOM / 'Editor').glob('*.cs')))
    items = []
    for path in files:
        methods = []
        for number, line in enumerate(path.read_text().splitlines(), 1):
            match = re.match(r'\s*(?:public|private|protected|internal)\s+(?:(?:static|override|sealed|async)\s+)*(?:[\w.<>,\[\]?]+\s+)+(\w+)\s*\(', line)
            if match:
                methods.append({'name': match[1], 'line': number})
        relative = path.relative_to(ROOT).as_posix()
        items.append({'file': relative, 'category': 'Editor only' if 'Editor' in path.parts else 'Runtime or optional runtime',
                      'sha256': hashlib.sha256(path.read_bytes()).hexdigest(), 'methods': methods,
                      'github_url': 'https://github.com/AndhikaNayakaAW/AndhikaNayakaAryaWibowo_DECO2300_2026/blob/' + commit + '/' + quote(relative)})
    output = {'source_commit': commit, 'prepared_on': '2026-10-21', 'custom_script_count': len(items), 'scripts': items}
    (PACK / 'Code Study/Code Index.json').write_text(json.dumps(output, indent=2) + '\n')
    guide = (PACK / 'Code Study/Code Study Guide.md').read_text()
    missing = [path.name for path in files if path.name not in guide]
    if missing:
        raise ValueError('Guide omitted custom scripts: ' + ', '.join(missing))
    print(f'Indexed and explained {len(items)} custom C# files')


if __name__ == '__main__':
    build_index()
    spec = importlib.util.spec_from_file_location('preparation_docs', Path(__file__).with_name('build_preparation_documents.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.build('Code Study/Code Study Guide.md')
