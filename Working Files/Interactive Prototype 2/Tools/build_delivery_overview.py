"""Create a short PDF file map and a PDF of the existing paper-prototype photo."""
from pathlib import Path
import pymupdf

ROOT = Path(__file__).resolve().parents[3]
PACK = ROOT / 'Interactive Prototype 2'
WORK = ROOT / 'Working Files/Interactive Prototype 2'
REGULAR = '/System/Library/Fonts/Supplemental/Times New Roman.ttf'
BOLD = '/System/Library/Fonts/Supplemental/Times New Roman Bold.ttf'
FONT = pymupdf.Font(fontfile=REGULAR)
MARGIN = 72
WIDTH, HEIGHT = 595.276, 841.89


def new_page(document):
    page = document.new_page(width=WIDTH, height=HEIGHT)
    page.insert_font(fontname='TNR', fontfile=REGULAR)
    page.insert_font(fontname='TNRB', fontfile=BOLD)
    page.insert_text((WIDTH - 82, 42), str(len(document)), fontname='TNR', fontsize=12)
    return page


def lines(text, width, size=12):
    output, current = [], ''
    for word in text.split():
        trial = (current + ' ' + word).strip()
        if current and FONT.text_length(trial, fontsize=size) > width:
            output.append(current)
            current = word
        else:
            current = trial
    if current:
        output.append(current)
    return output


def paragraph(page, text, y, bold=False, size=12, leading=24, width=None, x=MARGIN):
    for line in lines(text, width or WIDTH - 2 * MARGIN, size):
        if y > HEIGHT - MARGIN:
            raise ValueError('Overview page overflow')
        page.insert_text((x, y), line, fontname='TNRB' if bold else 'TNR', fontsize=size)
        y += leading
    return y


def title(page, text, y=84):
    font = pymupdf.Font(fontfile=BOLD)
    x = (WIDTH - font.text_length(text, fontsize=12)) / 2
    page.insert_text((x, y), text, fontname='TNRB', fontsize=12)
    return y + 32


def build_overview():
    document = pymupdf.open()
    page = new_page(document)
    y = title(page, 'Interactive Prototype 2')
    y = paragraph(page, 'Submission folder prepared on 21 October 2026. Documents are PDFs. '
                  'The MP4, APK, and Unity ZIP are retained because the assessment also requires '
                  'a working prototype, video, and source.', y) + 12
    rows = [
        ('Testing', 'Prospective Testing Plan and printable Participant Record PDFs.'),
        ('Design Journal', 'Design Journal PDF and the original paper-prototype photograph in PDF form.'),
        ('Evaluation', 'Design Evaluation 3 and Reflection PDF, including the supplied participant evidence.'),
        ('Research Evidence', 'The supplied XR Study Whiteboard Article Evidence PDF, copied unchanged.'),
        ('Code Study', 'The code study guide PDF explaining all 34 custom C# scripts.'),
        ('Earlier Evidence', 'Evaluation 1 and Evaluation 2 PDFs for the development history.'),
        ('Media', "Syifa's 1080p MP4 recording, about 5 minutes 38 seconds, with an audio track."),
        ('Builds', 'The existing Quest 2 v1.3 APK. No new app build was made during cleanup.'),
        ('Source Code', 'Whiteboard Unity Project.zip with Assets, Packages, and ProjectSettings.'),
    ]
    table_width = WIDTH - 2 * MARGIN
    page.draw_line((MARGIN, y - 12), (WIDTH - MARGIN, y - 12), color=(0, 0, 0), width=.7)
    y = paragraph(page, 'Folder', y, bold=True, size=11, leading=17)
    page.insert_text((MARGIN + 132, y - 17), 'Contents', fontname='TNRB', fontsize=11)
    page.draw_line((MARGIN, y - 11), (WIDTH - MARGIN, y - 11), color=(0, 0, 0), width=.5)
    y += 3
    for name, description in rows:
        start = y
        y1 = paragraph(page, name, start, size=11, leading=15, width=122)
        y2 = paragraph(page, description, start, size=11, leading=15,
                       width=table_width - 132, x=MARGIN + 132)
        y = max(y1, y2) + 7
    page.draw_line((MARGIN, y - 9), (WIDTH - MARGIN, y - 9), color=(0, 0, 0), width=.7)
    y = paragraph(page, 'Open the Unity project with Unity 6000.3.21f1. The editable project '
                  'remains in the repository folder named Whiteboard. The ZIP is a portable source snapshot.', y + 14)
    paragraph(page, 'Editable documents, spreadsheets, JSON indexes, helper scripts, and the original MOV '
              'are kept separately under Working Files/Interactive Prototype 2. They are outside this submission folder.', y + 10)

    page = new_page(document)
    y = title(page, 'Readiness and Submission')
    y = paragraph(page, 'Prepared', y, bold=True) + 5
    for text in ['The Unity source archive and existing APK are packaged.',
                 'The plan, journal, participant form, evaluations, research evidence, and code guide are PDFs.',
                 'Existing participant testing and the supplied Syifa video are retained. '
                 'The recording was opened and sampled. The full audio was not independently reviewed.']:
        y = paragraph(page, text, y) + 9
    y = paragraph(page, 'Before and during the studio', y + 6, bold=True) + 5
    for text in ['Submit the plan before the first studio and confirm the allocated session time.',
                 'Check the known working APK, charge equipment, and confirm that the marker can access the video.',
                 'Attend the mandatory in-person studio and record any additional testing requested.',
                 'Keep real session dates. Add new observations to the evaluation if needed, then submit '
                 'the required source, video, plan, journal, and outcomes by the end of the session.']:
        y = paragraph(page, text, y) + 9
    paragraph(page, 'The large MP4, APK, and source ZIP are stored locally and are excluded from Git. '
              'The tracked Unity source and PDF documents remain on GitHub. File preparation is not an upload confirmation.', y + 5)
    document.set_metadata({'title': 'Interactive Prototype 2: Start Here', 'author': 'Andhika Nayaka Arya Wibowo'})
    document.save(PACK / 'START HERE.pdf', garbage=4, deflate=True)


def build_photo():
    document = pymupdf.open()
    page = new_page(document)
    y = title(page, 'Paper Prototype Photograph')
    y = paragraph(page, 'Existing paper classroom prototype retained as visual evidence of the early design.', y)
    image = WORK / 'Design Journal/Paper Prototype Photo.jpeg'
    page.insert_image(pymupdf.Rect(MARGIN, y + 12, WIDTH - MARGIN, HEIGHT - MARGIN - 34),
                      filename=str(image), keep_proportion=True)
    paragraph(page, 'Source: the existing Week 2 paper-prototype project record.', HEIGHT - MARGIN - 6, size=10, leading=12)
    document.set_metadata({'title': 'Paper Prototype Photograph', 'author': 'Andhika Nayaka Arya Wibowo'})
    document.save(PACK / 'Design Journal/Paper Prototype Photo.pdf', garbage=4, deflate=True)


if __name__ == '__main__':
    build_overview()
    build_photo()
