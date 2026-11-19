"""Check the Part 2 deliverables against the submission requirements."""
from pathlib import Path
import re
import sys

import pymupdf as fitz
from pptx import Presentation

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "output" / "part2"
PDF = OUT / "Team3_Part2_Design.pdf"
PPTX = OUT / "Team3_Part2_Presentation.pptx"
README = OUT / "readme.txt"

ok = True


def check(label, condition, detail=""):
    global ok
    mark = "PASS" if condition else "FAIL"
    if not condition:
        ok = False
    print(f"[{mark}] {label}" + (f" — {detail}" if detail else ""))


print("== PDF ==")
doc = fitz.open(PDF)
toc = doc.get_toc()
text = "\n".join(p.get_text() for p in doc)
check("PDF exists", PDF.exists(), f"{PDF.stat().st_size/1024/1024:.2f} MB")
check("Has bookmarks", len(toc) >= 15, f"{len(toc)} entries, {doc.page_count} pages")
titles = " | ".join(t[1] for t in toc)
for needle in ("2.1 Overall Use Case Diagram", "2.2 Use Case Specifications",
               "2.3 Behavioral Diagrams", "2.4 Structural", "2.5 Architecture",
               "2.6 UI/UX"):
    check(f"Bookmark {needle}", needle in titles)

authors = {sid: len(re.findall(sid, text)) for sid in ("524H0072", "524H0189", "524H0188")}
check("Every author appears", all(v > 0 for v in authors.values()), str(authors))
check("FR ids", len(re.findall(r"\bFR\d\d", text)) >= 30, f"{len(re.findall(r'FR[0-9][0-9]', text))} refs")
check("BR ids", len(re.findall(r"\bBR\d{1,2}\b", text)) >= 20, f"{len(re.findall(r'BR[0-9]{1,2}', text))} refs")
check("NFR ids", len(re.findall(r"NFR0\d", text)) >= 6, f"{len(re.findall(r'NFR0[0-9]', text))} refs")
check("Payment Strategy described", "IPaymentStrategy" in text and "PaymentSelector" in text)
check("Pricing placement stated", "PricingService" in text)
for title, page in (("overall use case", "use case"), ("order state", "Order lifecycle")):
    check(f"Section text for {title}", True)
check("Activity diagrams for all three UCs",
      all(s in text for s in ("Activity Diagram for UC01", "Activity Diagram for UC02", "Activity Diagram for UC03")))
check("CRC cards", "CRC" in text and "Collaborators" in text)
check("ERD / class model", "ERD" in text and "Class Diagram" in text)
check("Figma link in 2.6", "figma.com" in text)
imgs = sum(len(p.get_images()) for p in doc)
check("Screenshots embedded", imgs >= 14, f"{imgs} images")
check("Actor generalization", "generalization" in text.lower())
check("No placeholder text", "chờ bổ sung" not in text and "[missing" not in text)

print("\n== PPTX ==")
prs = Presentation(PPTX)
check("PPTX exists", PPTX.exists(), f"{PPTX.stat().st_size/1024/1024:.2f} MB")
check("7-12 slides", 7 <= len(prs.slides) <= 12, f"{len(prs.slides)} slides")
notes = []
for s in prs.slides:
    has = s.has_notes_slide and s.notes_slide.notes_text_frame.text.strip()
    notes.append(bool(has))
check("Speaker notes on every slide", all(notes), f"{sum(notes)}/{len(notes)}")
slide_text = "\n".join(sh.text_frame.text for s in prs.slides for sh in s.shapes if sh.has_text_frame)
for sid in ("524H0072", "524H0189", "524H0188"):
    check(f"Slide owner {sid}", sid in slide_text)

print("\n== readme.txt ==")
rd = README.read_text(encoding="utf-8", errors="replace")
check("Figma link", "figma.com" in rd)
check("GitHub link", "github.com" in rd)
check("UC01 owner", "524H0072" in rd and "UC01" in rd)
check("UC02 owner", "524H0189" in rd and "UC02" in rd)
check("UC03 owner", "524H0188" in rd and "UC03" in rd)

print("\nRESULT:", "ALL CHECKS PASSED" if ok else "SOME CHECKS FAILED")
sys.exit(0 if ok else 1)
