"""Build Team3_Part2_Design.docx and its bookmarked PDF.

Content sources: docs/part2/*.md, docs/part2/uc01/*, rendered diagrams and V2 previews.
The official template supplies styles and page setup; the body is rebuilt for Part 2 only.
"""
from __future__ import annotations

import os
import shutil
import sys
from pathlib import Path

import fitz  # PyMuPDF, also used to rasterise SVG evidence
from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.opc.constants import RELATIONSHIP_TYPE as RT
from docx.shared import Cm, Pt, RGBColor

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE = ROOT / "outputs" / "instructor-source" / "SE_Sem01_2627_Report_Template.docx"
if not TEMPLATE.exists():
    TEMPLATE = Path(r"C:\Users\rumno\OneDrive\Desktop\drive-download-20261117T120953Z-1-001\SE_Sem01_2627_Report_Template.docx")
OUT_DIR = ROOT / "output" / "part2"
BUILD = ROOT / "tmp" / "part2build"
DOCX = BUILD / "Team3_Part2_Design.docx"
PDF = OUT_DIR / "Team3_Part2_Design.pdf"

FIGMA_FILE = "https://www.figma.com/design/Ao6NzrJvoRxJevn1gpn9QP"
FIGMA_PROTO = "https://www.figma.com/proto/Ao6NzrJvoRxJevn1gpn9QP"
GITHUB = "https://github.com/rumnopro0809/saigonbite-group3"

DIAG = ROOT / "docs" / "part2" / "diagrams"
PREV = ROOT / "outputs" / "saigonbite-v2" / "previews"
STAGES = ROOT / "outputs" / "saigonbite-v2" / "delivery" / "UC01 - Cac giai doan"
UC01IMG = ROOT / "docs" / "part2" / "uc01" / "images"
LOGO = ROOT / "outputs" / "saigonbite-v2" / "assets" / "logo.png"

NAVY = RGBColor(0x1F, 0x3A, 0x5F)
GREY = RGBColor(0x59, 0x59, 0x59)


# --------------------------------------------------------------------------- helpers
def clear_body(doc: Document) -> None:
    body = doc.element.body
    for child in list(body):
        if child.tag == qn("w:sectPr"):
            continue
        body.remove(child)


def set_cell_bg(cell, hex_fill: str) -> None:
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:fill"), hex_fill)
    tc_pr.insert_element_before(
        shd, "w:noWrap", "w:tcMar", "w:textDirection", "w:tcFitText", "w:vAlign", "w:hideMark"
    )


def set_table_borders(table, color: str = "A6A6A6", size: str = "4") -> None:
    tbl_pr = table._tbl.tblPr
    borders = OxmlElement("w:tblBorders")
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        el = OxmlElement(f"w:{edge}")
        el.set(qn("w:val"), "single")
        el.set(qn("w:sz"), size)
        el.set(qn("w:space"), "0")
        el.set(qn("w:color"), color)
        borders.append(el)
    tbl_pr.insert_element_before(
        borders, "w:shd", "w:tblLayout", "w:tblCellMar", "w:tblLook", "w:tblCaption", "w:tblDescription"
    )


def no_borders(table) -> None:
    tbl_pr = table._tbl.tblPr
    borders = OxmlElement("w:tblBorders")
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        el = OxmlElement(f"w:{edge}")
        el.set(qn("w:val"), "none")
        el.set(qn("w:sz"), "0")
        borders.append(el)
    tbl_pr.insert_element_before(
        borders, "w:shd", "w:tblLayout", "w:tblCellMar", "w:tblLook", "w:tblCaption", "w:tblDescription"
    )


def set_col_widths(table, widths_cm: list[float]) -> None:
    table.autofit = False
    for row in table.rows:
        for idx, width in enumerate(widths_cm):
            if idx < len(row.cells):
                row.cells[idx].width = Cm(width)


def add_hyperlink(paragraph, url: str, text: str | None = None, size: float = 9.5,
                  color: str = "0B5394", underline: bool = True):
    part = paragraph.part
    r_id = part.relate_to(url, RT.HYPERLINK, is_external=True)
    hyperlink = OxmlElement("w:hyperlink")
    hyperlink.set(qn("r:id"), r_id)

    new_run = OxmlElement("w:r")
    r_pr = OxmlElement("w:rPr")
    r_fonts = OxmlElement("w:rFonts")
    r_fonts.set(qn("w:ascii"), "Calibri")
    r_fonts.set(qn("w:hAnsi"), "Calibri")
    r_pr.append(r_fonts)
    sz = OxmlElement("w:sz")
    sz.set(qn("w:val"), str(int(size * 2)))
    r_pr.append(sz)
    c = OxmlElement("w:color")
    c.set(qn("w:val"), color)
    r_pr.append(c)
    if underline:
        u = OxmlElement("w:u")
        u.set(qn("w:val"), "single")
        r_pr.append(u)
    new_run.append(r_pr)

    t = OxmlElement("w:t")
    t.text = text or url
    new_run.append(t)
    hyperlink.append(new_run)
    paragraph._p.append(hyperlink)
    return hyperlink


def cell_hyperlink(cell, url: str, size: float = 8.5):
    cell.text = ""
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(1)
    add_hyperlink(p, url, url, size=size)


def cell_text(cell, text: str, bold: bool = False, size: float = 9.0, color=None, align=None):
    cell.text = ""
    lines = str(text).split("\n")
    for i, line in enumerate(lines):
        p = cell.paragraphs[0] if i == 0 else cell.add_paragraph()
        p.paragraph_format.space_before = Pt(0)
        p.paragraph_format.space_after = Pt(1)
        if align is not None:
            p.alignment = align
        run = p.add_run(line)
        run.font.size = Pt(size)
        run.font.name = "Calibri"
        run.bold = bold
        if color is not None:
            run.font.color.rgb = color


def add_table(doc: Document, headers: list[str], rows: list[list[str]], widths: list[float],
              header_fill: str = "1F3A5F", font_size: float = 9.0):
    table = doc.add_table(rows=1, cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(table)
    hdr = table.rows[0].cells
    for i, h in enumerate(headers):
        cell_text(hdr[i], h, bold=True, size=font_size, color=RGBColor(0xFF, 0xFF, 0xFF))
        set_cell_bg(hdr[i], header_fill)
    for r in rows:
        cells = table.add_row().cells
        for i, value in enumerate(r):
            cell_text(cells[i], value, size=font_size)
    set_col_widths(table, widths)
    keep_rows_together(table)
    doc.add_paragraph().paragraph_format.space_after = Pt(2)
    return table


def keep_rows_together(table) -> None:
    for idx, row in enumerate(table.rows):
        tr_pr = row._tr.get_or_add_trPr()
        tr_pr.append(OxmlElement("w:cantSplit"))
        if idx == 0:
            tr_pr.append(OxmlElement("w:tblHeader"))


def add_keyvalue_table(doc: Document, rows: list[tuple[str, str]], label_w: float = 4.0):
    table = doc.add_table(rows=0, cols=2)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(table)
    for label, value in rows:
        cells = table.add_row().cells
        cell_text(cells[0], label, bold=True, size=9)
        set_cell_bg(cells[0], "EAF0F6")
        cell_text(cells[1], value, size=9)
    set_col_widths(table, [label_w, 16.5 - label_w])
    keep_rows_together(table)
    doc.add_paragraph().paragraph_format.space_after = Pt(2)
    return table


def bullet(doc: Document, text: str, bold_lead: str | None = None):
    p = doc.add_paragraph(style="List Paragraph")
    p.paragraph_format.left_indent = Cm(0.6)
    p.paragraph_format.first_line_indent = Cm(-0.35)
    p.paragraph_format.space_after = Pt(2)
    run = p.add_run("• ")
    run.font.size = Pt(10.5)
    if bold_lead:
        r = p.add_run(bold_lead)
        r.bold = True
        r.font.size = Pt(10.5)
    r = p.add_run(text)
    r.font.size = Pt(10.5)


def para(doc: Document, text: str, size: float = 10.5, italic: bool = False, space_after: float = 4):
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(space_after)
    p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    r = p.add_run(text)
    r.font.size = Pt(size)
    r.italic = italic
    return p


def add_image(doc: Document, path: Path, width_cm: float, caption: str | None = None,
              max_height_cm: float = 19.5):
    if not path.exists():
        para(doc, f"[missing image: {path.name}]", italic=True)
        return
    with fitz.open(path) as d:
        rect = d[0].rect
    height_cm = width_cm * rect.height / rect.width
    if height_cm > max_height_cm:
        width_cm = max_height_cm * rect.width / rect.height
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(4)
    p.paragraph_format.space_after = Pt(2)
    p.paragraph_format.keep_with_next = True
    p.add_run().add_picture(str(path), width=Cm(width_cm))
    if caption:
        c = doc.add_paragraph()
        c.alignment = WD_ALIGN_PARAGRAPH.CENTER
        c.paragraph_format.space_after = Pt(8)
        r = c.add_run(caption)
        r.italic = True
        r.font.size = Pt(8.5)
        r.font.color.rgb = GREY


def add_image_grid(doc: Document, items: list[tuple[Path, str]], width_cm: float = 7.9,
                   max_height_cm: float = 8.6):
    """Two images per row, captions underneath, borderless layout table."""
    table = doc.add_table(rows=0, cols=2)
    no_borders(table)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    for i in range(0, len(items), 2):
        cells = table.add_row().cells
        table.rows[-1]._tr.get_or_add_trPr().append(OxmlElement("w:cantSplit"))
        for j, (path, caption) in enumerate(items[i:i + 2]):
            cell = cells[j]
            cell.text = ""
            p = cell.paragraphs[0]
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            if path.exists():
                with fitz.open(path) as d:
                    rect = d[0].rect
                w = width_cm
                if w * rect.height / rect.width > max_height_cm:
                    w = max_height_cm * rect.width / rect.height
                p.add_run().add_picture(str(path), width=Cm(w))
            else:
                p.add_run(f"[missing {path.name}]")
            cp = cell.add_paragraph()
            cp.alignment = WD_ALIGN_PARAGRAPH.CENTER
            r = cp.add_run(caption)
            r.italic = True
            r.font.size = Pt(8)
            r.font.color.rgb = GREY
    set_col_widths(table, [8.25, 8.25])
    doc.add_paragraph().paragraph_format.space_after = Pt(2)


def add_toc(doc: Document):
    p = doc.add_paragraph()
    run = p.add_run()
    fld_begin = OxmlElement("w:fldChar")
    fld_begin.set(qn("w:fldCharType"), "begin")
    instr = OxmlElement("w:instrText")
    instr.set(qn("xml:space"), "preserve")
    instr.text = 'TOC \\o "1-3" \\h \\z \\u'
    fld_sep = OxmlElement("w:fldChar")
    fld_sep.set(qn("w:fldCharType"), "separate")
    placeholder = OxmlElement("w:t")
    placeholder.text = "Table of contents will be generated when the document is opened in Word."
    fld_end = OxmlElement("w:fldChar")
    fld_end.set(qn("w:fldCharType"), "end")
    for el in (fld_begin, instr, fld_sep, placeholder, fld_end):
        run._r.append(el)


def setup_header_footer(doc: Document):
    section = doc.sections[0]
    section.top_margin = Cm(2.0)
    section.bottom_margin = Cm(2.0)

    header = section.header
    header.is_linked_to_previous = False
    hp = header.paragraphs[0]
    hp.text = ""
    hp.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    r = hp.add_run("SaigonBite  •  Team 3  •  Part 2: Software Design, Architecture & UI/UX")
    r.font.size = Pt(8)
    r.font.color.rgb = GREY

    footer = section.footer
    footer.is_linked_to_previous = False
    fp = footer.paragraphs[0]
    fp.text = ""
    fp.alignment = WD_ALIGN_PARAGRAPH.CENTER

    def field(fld: str) -> None:
        r = fp.add_run()
        b = OxmlElement("w:fldChar")
        b.set(qn("w:fldCharType"), "begin")
        i = OxmlElement("w:instrText")
        i.set(qn("xml:space"), "preserve")
        i.text = fld
        sep = OxmlElement("w:fldChar")
        sep.set(qn("w:fldCharType"), "separate")
        txt = OxmlElement("w:t")
        txt.text = "1"
        end = OxmlElement("w:fldChar")
        end.set(qn("w:fldCharType"), "end")
        for el in (b, i, sep, txt, end):
            r._r.append(el)

    r = fp.add_run("SaigonBite — Team 3 — Sem01_2627 — Page ")
    r.font.size = Pt(8)
    r.font.color.rgb = GREY
    field("PAGE")
    r2 = fp.add_run(" of ")
    r2.font.size = Pt(8)
    r2.font.color.rgb = GREY
    field("NUMPAGES")


def svg_to_png(src: Path, dst: Path, scale: float = 2.0) -> Path:
    if dst.exists() and dst.stat().st_mtime >= src.stat().st_mtime:
        return dst
    with fitz.open(src) as d:
        pix = d[0].get_pixmap(matrix=fitz.Matrix(scale, scale), alpha=False)
        pix.save(dst)
    return dst


# --------------------------------------------------------------------------- content
def cover(doc: Document):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(24)
    if LOGO.exists():
        p.add_run().add_picture(str(LOGO), width=Cm(4.2))

    t = doc.add_paragraph()
    t.alignment = WD_ALIGN_PARAGRAPH.CENTER
    t.paragraph_format.space_before = Pt(10)
    t.paragraph_format.space_after = Pt(0)
    r = t.add_run("SAIGONBITE")
    r.bold = True
    r.font.size = Pt(38)
    r.font.color.rgb = NAVY
    r.font.name = "Calibri Light"

    st = doc.add_paragraph()
    st.alignment = WD_ALIGN_PARAGRAPH.CENTER
    st.paragraph_format.space_after = Pt(2)
    r = st.add_run("Part 2 — Software Design, Architecture & UI/UX")
    r.bold = True
    r.font.size = Pt(17)
    r.font.color.rgb = NAVY

    sub = doc.add_paragraph()
    sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
    sub.paragraph_format.space_after = Pt(28)
    r = sub.add_run("Food ordering and delivery from cloud-kitchen hubs in Ho Chi Minh City")
    r.italic = True
    r.font.size = Pt(11)
    r.font.color.rgb = GREY

    info = doc.add_table(rows=0, cols=2)
    info.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(info, color="C9D3DD")
    rows = [
        ("Team", "Team 3"),
        ("Chosen tier", "Tier 3 — ASP.NET MVC, Entity Framework Code First, SQL Server, Bootstrap"),
        ("Document version", "v1.0"),
        ("Date", "20 November 2026"),
        ("Repository", GITHUB),
        ("Figma prototype (V2)", FIGMA_PROTO),
        ("Figma design file", FIGMA_FILE),
    ]
    for k, v in rows:
        cells = info.add_row().cells
        cell_text(cells[0], k, bold=True, size=9.5)
        set_cell_bg(cells[0], "EAF0F6")
        if v.startswith("http"):
            cell_hyperlink(cells[1], v, size=8.5)
        else:
            cell_text(cells[1], v, size=9.5)
    set_col_widths(info, [4.2, 11.0])

    doc.add_paragraph().paragraph_format.space_after = Pt(6)

    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(6)
    r = p.add_run("By Students:")
    r.bold = True
    r.font.size = Pt(11)
    for line in (
        "1. 524H0072 : Nguyễn Hải Triều  —  UC01 Place and Manage Orders",
        "2. 524H0189 : Dương Phạm Thái  —  UC02 Manage Menu and Brands",
        "3. 524H0188 : Lê Quang Tấn  —  UC03 Manage Hubs and Capacity",
    ):
        pp = doc.add_paragraph()
        pp.alignment = WD_ALIGN_PARAGRAPH.CENTER
        pp.paragraph_format.space_after = Pt(0)
        r = pp.add_run(line)
        r.font.size = Pt(10.5)

    doc.add_page_break()


def front_matter(doc: Document):
    doc.add_heading("Intellectual Property", level=1)
    para(doc, "Copyright 2026 Team 3 (SaigonBite). The documentation, its content and its presentation are "
              "proprietary to Team 3 and embody the team's design work. All copyright, trademarks, trade names and "
              "other intellectual property rights contained herein remain the exclusive property of Team 3 unless "
              "otherwise specified. The information is provided for assessment of the SE Final Project and may not be "
              "reproduced or disclosed without permission.")

    doc.add_heading("Table of Contents", level=1)
    add_toc(doc)
    doc.add_page_break()

    doc.add_heading("Feedback Log", level=1)
    para(doc, "No Part 2 presentation or marking feedback has been received at the time of writing. The table is kept "
              "so that any instructor comment can be recorded with its date and the exact place where the design was "
              "changed.")
    add_table(doc,
              ["Source", "Feedback received", "What we changed and where", "Done"],
              [["Part 1 marking", "No marking feedback recorded yet", "To be updated when feedback is received", "—"],
               ["Part 2 Design Review", "No feedback recorded yet", "To be updated after the presentation", "—"]],
              [3.2, 4.4, 7.4, 1.5])
    doc.add_page_break()


def part2_intro(doc: Document):
    doc.add_heading("Part 2: Software Design, Architecture & UI/UX (20 marks)", level=1)
    para(doc, "This document translates the Part 1 requirements of SaigonBite into a Tier 3 design. It covers the shared "
              "design (overall use cases, order lifecycle, structure, database, architecture and UI/UX) and the three "
              "owned CRUD use cases. Requirement identifiers FR01–FR19, NFR01–NFR06 and BR1–BR18 keep the meanings "
              "defined in Part 1; they are not renumbered here.")
    para(doc, "Scope of this submission: UC01 Place and Manage Orders (524H0072), UC02 Manage Menu and Brands "
              "(524H0189) and UC03 Manage Hubs and Capacity (524H0188), together with order lifecycle HT03, accounts "
              "HT01, vouchers HT02 and reports HT04. Payments are simulated under Tier 3; no live money is moved. "
              "Implementation and test execution belong to Part 3 and are not claimed here.")
    para(doc, "How to read the document: each use case section states the actor, pre-conditions, main flow, alternate "
              "and exception flows, post-conditions and the FR/BR identifiers that constrain it. Every diagram and "
              "model names its author (student ID) so individual work can be assessed.")
    add_table(doc,
              ["Section", "Content", "Owner"],
              [["2.1", "Overall use case diagram with actor generalization", "Team"],
               ["2.2", "Use case specifications with FR/BR references", "524H0072 / 524H0189 / 524H0188"],
               ["2.3", "Activity diagram per use case and the shared order lifecycle", "Per use case / team"],
               ["2.4", "CRC cards, class model and ERD with the payment Strategy pattern", "524H0072 / 524H0189 / 524H0188"],
               ["2.5", "MVC architecture, principles and pricing placement", "Team"],
               ["2.6", "Four-stage Figma prototype, screenshots and public link", "Team"]],
              [2.0, 10.5, 4.0], font_size=8.5)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(6)
    r = p.add_run("Design goals for this submission")
    r.bold = True
    r.font.size = Pt(10.5)
    for text in (
        "Make every price reproducible: the same order and the same placement snapshot must always produce the "
        "same breakdown (BR3–BR11).",
        "Keep capacity under control: a hub at or above 90% load refuses new orders, and load is always derived "
        "from real orders (BR8).",
        "Protect money and history: payments are simulated, refunds are recorded, and no order, payment or "
        "status history is hard-deleted (BR15–BR17).",
        "Separate responsibilities so each use case can be explained and tested on its own (NFR02).",
        "Design for both customer groups and both screen sizes without duplicating the ordering flow (NFR03).",
    ):
        bullet(doc, text)
    doc.add_page_break()


def section_2_1(doc: Document):
    doc.add_heading("2.1 Overall Use Case Diagram", level=1)
    add_image(doc, DIAG / "overall-use-case.png", 10.2,
              "Figure 1 — Overall use case diagram with actor generalization (team model: 524H0072, 524H0189, 524H0188).")
    para(doc, "Customer is an abstract actor specialized by Local Customer and Foreign Tourist. The specialization "
              "captures the differences in registration data, language and permitted payment methods (BR13) without "
              "duplicating the ordering use case. Kitchen Staff, Shipper and System Admin are separate operational "
              "actors because their permissions and responsibilities differ.")
    para(doc, "The three assessed use cases are UC01, UC02 and UC03. UC01 includes the four CRUD behaviours create, "
              "read/track, edit and cancel. UC02 covers brand and menu-item management. UC03 covers hub, capacity and "
              "brand-membership management. HT01 accounts, HT02 vouchers, HT03 kitchen/delivery operations and HT04 "
              "reports are supporting functions required by the specification and are referenced where a use case "
              "depends on them.")


def use_case_table(doc: Document, rows: list[tuple[str, str]]):
    add_keyvalue_table(doc, rows, label_w=4.2)


def section_2_2(doc: Document):
    doc.add_heading("2.2 Use Case Specifications", level=1)
    para(doc, "The three specifications below use the same structure required by the report template. The FR/BR "
              "identifiers are the traceability codes from Part 1; the full Vietnamese working specification and the "
              "ambiguity log for UC01 are kept in the repository.")

    # UC01
    doc.add_heading("2.2.1 Use Case 1: Place and Manage Orders (author: 524H0072)", level=2)
    use_case_table(doc, [
        ("Name / ID", "Place and Manage Orders — UC01"),
        ("Primary actor", "Customer, specialized into Local Customer and Foreign Tourist."),
        ("Supporting actors", "Kitchen Staff and Shipper through HT03; System Admin for operational cancellation."),
        ("Requirements", "FR01–FR07, FR09, FR13, FR15–FR17; BR1–BR17; NFR01–NFR06. Report consumers R1/R2."),
        ("Trigger", "The customer starts a new order from a selected hub, opens order history, or requests an allowed "
                    "edit or cancellation."),
        ("Pre-conditions", "The account is authenticated. Hub, brand, menu and capacity data exist. For an existing "
                           "order the customer owns it. For a new order the selected hub is active."),
        ("Main flow — create",
         "1. Customer selects an active hub and browses its supported brands and Available items (FR09/BR12).\n"
         "2. Customer adds 1–30 lines with 1–20 units per line, enters the delivery address and distance, and may "
         "apply one voucher.\n"
         "3. System reads placement time, rain flag, current Confirmed/Preparing count and capacity. It rejects a "
         "subtotal below 50,000 VND, distance over 8.0 km, invalid lines or hub load ≥ 90% (BR1/BR2/BR8/BR12).\n"
         "4. PricingService calculates S, Q, V, F, D, R and RainFee in the BR10 order, rounds once at the end and "
         "returns the breakdown (BR3–BR11, FR03).\n"
         "5. System offers only the methods allowed for the customer type: MoMo, VNPay, COD for Local; Apple Pay, "
         "PayPal, COD for Tourist. The server rechecks BR13 and the rounded COD limit of 1,000,000 VND (BR14).\n"
         "6. Customer confirms. The authoritative inputs are revalidated; a changed quote is returned for "
         "confirmation instead of charging a different amount.\n"
         "7. PaymentSelector invokes the matching simulated strategy. COD is recorded as awaiting collection; online "
         "methods return a simulated success or failure.\n"
         "8. On success the system stores the Order in Placed, its lines, the immutable price/condition snapshot, the "
         "PaymentRecord and the first history entry in one transaction (FR01, NFR05/06)."),
        ("Alternate flows",
         "A1 Read and track: the customer opens only owned orders and sees stored lines, prices, breakdown, payment "
         "result and status history (FR05). Kitchen/shipper updates from HT03 appear in tracking; the customer never "
         "sets a lifecycle state directly.\n"
         "A2 Edit: allowed only while the order is Placed (BR17). Item lines may change; the hub stays fixed. Existing "
         "line prices are kept and new lines use current prices (AMB03). Recalculation reuses the placement time, load "
         "and rain snapshot. A price increase or decrease is collected or refunded in simulation before commit "
         "(AMB05); on failure the original order is preserved. A rowversion conflict is rejected and reloaded (FR06).\n"
         "A3 Cancel: allowed in Placed or Confirmed only (BR16). The order becomes Cancelled, history is appended, and "
         "an online-paid amount is fully refunded in simulation. Order, lines and payment history are retained; no hard "
         "delete (FR07). From Preparing onward cancellation is refused."),
        ("Exception flows",
         "E1 Invalid input or voucher: actionable field errors, no order created.\n"
         "E2 Hub inactive or load ≥ 90%: the order is refused and other hubs are suggested (BR8).\n"
         "E3 Method not allowed for the actor, or COD above the rounded cap: rejected server-side (BR13/BR14).\n"
         "E4 Order not owned, or not visible to the actor: denied without disclosing data (NFR04).\n"
         "E5 Payment or storage failure: no partial order is left behind; diagnostics are logged without secrets.\n"
         "E6 Duplicate submission: the unique customer/request key returns the already-created order rather than "
         "charging twice.\n"
         "E7 Concurrent state change (for example Confirmed while editing): the stale mutation is rejected."),
        ("Post-conditions",
         "Create leaves exactly one Placed order with valid lines, reproducible pricing, payment status and an audit "
         "entry. Read returns owned records only and mutates nothing. Edit leaves the state unchanged, keeps the "
         "original context and preserves the old amount if the adjustment fails. Cancel records the permitted "
         "transition, the full simulated refund and the retained history. A rejected action leaves no partial or "
         "unauthorized change."),
    ])

    # UC02
    doc.add_heading("2.2.2 Use Case 2: Manage Menu and Brands (author: 524H0189)", level=2)
    use_case_table(doc, [
        ("Name / ID", "Manage Menu and Brands — UC02"),
        ("Primary actor", "System Admin. Customer browsing is a related read flow that consumes this data."),
        ("Requirements", "FR08–FR11, FR09, FR01, FR13; BR12; R1 as report consumer."),
        ("Trigger", "Admin opens brand or menu-item management."),
        ("Pre-conditions", "The Admin is authenticated and authorized server-side. The database is available. A menu "
                           "item references an existing brand, and a brand can be linked to serving hubs through "
                           "HubBrand."),
        ("Main flow — create",
         "1. Admin chooses Add Brand, enters name and category, and saves; the system validates required fields and "
         "stores an active brand (FR08).\n"
         "2. Admin chooses Add Item, selects brand and category, enters name, unit price and availability (FR08).\n"
         "3. System validates fields and the decimal monetary value, saves the item and returns the refreshed list.\n"
         "4. Admin or hub management links the brand to hubs; an item cannot be ordered from a hub that does not "
         "serve its brand (BR12)."),
        ("Alternate flows",
         "A1 Read: Admin filters and pages brands/items and opens details, including availability and deleted status "
         "(FR09). Customers see only active brands and items served by the selected hub.\n"
         "A2 Update: Admin edits metadata, category, price or availability. The server rechecks authorization, the "
         "referenced brand and the rowversion. Future orders use the new price; stored order lines keep their snapshot "
         "prices (FR10).\n"
         "A3 Delete: Admin deactivates or soft-deletes a brand or item and confirms. Foreign keys and historical order "
         "names, categories and prices remain; the item or brand becomes ineligible for new orders (FR11)."),
        ("Exception flows",
         "E1 Missing fields or a negative price: field errors, nothing saved.\n"
         "E2 Unauthorized actor: rejected server-side (FR13/NFR04).\n"
         "E3 Stale edit: the rowversion mismatch forces a reload instead of overwriting.\n"
         "E4 Item becomes unavailable while a stale page is open: OrderService rejects the order (BR12).\n"
         "E5 Soft-deleted references: still queryable for historical reports, never resurrected silently."),
        ("Post-conditions",
         "Create persists a correctly linked brand or item. Read returns the correctly scoped catalog. Update changes "
         "future availability or price while preserving old snapshots. Delete removes new-order eligibility while "
         "retaining historical references. No operation grants a customer Admin write access."),
    ])

    # UC03
    doc.add_heading("2.2.3 Use Case 3: Manage Hubs and Capacity (author: 524H0188)", level=2)
    use_case_table(doc, [
        ("Name / ID", "Manage Hubs and Capacity — UC03"),
        ("Primary actor", "System Admin."),
        ("Requirements", "FR14.1–FR14.4, FR02, FR16, FR19; BR5, BR7, BR8, BR17; R2 as report consumer; AMB06."),
        ("Trigger", "Admin opens hub management."),
        ("Pre-conditions", "The Admin is authenticated and authorized server-side. Brands exist for membership "
                           "selection. Capacity is a positive whole number (team assumption supporting the BR8 "
                           "denominator)."),
        ("Main flow — create",
         "1. Admin enters hub name, location/address, positive integer kitchen capacity and supported brands.\n"
         "2. Server validates capacity > 0 and integer membership references.\n"
         "3. Hub and HubBrand links are saved in one transaction; the new hub is displayed with zero derived load when "
         "no active orders exist (FR14.1)."),
        ("Alternate flows",
         "A1 Read: Admin views hub list and details. HubLoadService counts only Confirmed and Preparing orders and "
         "divides by capacity; load is derived and never typed in. R2 additionally shows handled count, the valid "
         "Confirmed-to-Ready mean and granted Quiet Kitchen counts under AMB06.\n"
         "A2 Update: Admin changes location, capacity or supported brands; capacity is rechecked as a positive "
         "integer and references are validated. A lower capacity may push derived load above 100% but must not rewrite "
         "existing order states or historical placement load (FR14.2/FR16).\n"
         "A3 Delete: Admin deactivates or soft-deletes a hub. New orders stop; existing kitchen and shipper work can "
         "finish; orders, payments and history are not cascade-deleted (FR14.4)."),
        ("Exception flows",
         "E1 Capacity ≤ 0 or non-integer: rejected with a field error.\n"
         "E2 Unknown brand reference: rejected; nothing is saved.\n"
         "E3 Derived load above 100% after a capacity cut: displayed, but no historic recalculation.\n"
         "E4 Inactive hub: new orders refused (BR8); existing orders remain readable and processable.\n"
         "E5 Stale edit: rowversion mismatch forces reload."),
        ("Post-conditions",
         "Hub and membership are persisted consistently. Load is always derived from current orders and capacity. "
         "Capacity, brand-membership and rain-flag changes affect only new placements and never rewrite snapshots or "
         "financial history."),
    ])


def section_2_3(doc: Document):
    doc.add_heading("2.3 Behavioral Diagrams", level=1)
    para(doc, "Each owned use case has an activity diagram; the order lifecycle is one shared state diagram. The "
              "detailed CRUD activity diagrams for UC01 (create, read, edit, cancel) are included in Appendix C.")

    doc.add_heading("2.3.1 Activity Diagram for UC01 Place and Manage Orders (author: 524H0072)", level=2)
    add_image(doc, DIAG / "uc01-order-activity.png", 15.0,
              "Figure 2 — UC01 overview: customer and system lanes for create, read, edit and cancel (author: 524H0072).")
    para(doc, "The overview shows the four CRUD branches behind one customer entry point. Create validates the hub, "
              "the menu and the load before pricing; edit is reachable only in Placed and reuses the original "
              "snapshot; cancel is reachable in Placed or Confirmed and always records the refund outcome.")

    doc.add_heading("2.3.2 Activity Diagram for UC02 Manage Menu and Brands (author: 524H0189)", level=2)
    add_image(doc, DIAG / "uc02-menu-activity.png", 15.5,
              "Figure 3 — UC02 activity: create, read, update and soft-delete of brands and menu items (author: 524H0189).")

    doc.add_heading("2.3.3 Activity Diagram for UC03 Manage Hubs and Capacity (author: 524H0188)", level=2)
    add_image(doc, DIAG / "uc03-hub-activity.png", 15.5,
              "Figure 4 — UC03 activity: hub CRUD, capacity validation and derived load (author: 524H0188).")

    doc.add_heading("2.3.4 Order State Diagram (team)", level=2)
    add_image(doc, DIAG / "order-state.png", 10.5,
              "Figure 5 — Order lifecycle: eight states and eight business transitions (team: 524H0072, 524H0189, 524H0188; BR15–BR17).",
              max_height_cm=17.5)
    para(doc, "The eight states are Placed, Confirmed, Preparing, Ready, Out for Delivery, Delivered, Cancelled and "
              "Failed. Only the eight transitions in the diagram are permitted; Delivered, Cancelled and Failed are "
              "terminal. Guards name the actor and the ownership condition (own hub, assigned shipper, own order). "
              "Editing items never changes the state, and the policy for assigning a shipper is a documented "
              "assumption (AMB01), not an extra state.")


def section_2_4(doc: Document):
    doc.add_heading("2.4 Structural & CRC Design", level=1)
    doc.add_heading("2.4.1 CRC Cards", level=2)
    para(doc, "The cards list the class, its responsibilities and its collaborators. Owners are shown per class; the "
              "full responsibility notes are kept in the repository (docs/part2/crc-cards.md).")
    add_table(doc,
              ["Class (owner)", "Responsibilities", "Collaborators"],
              [["OrderService (524H0072)",
                "Orchestrates authoritative placement validation, pricing, payment, edit and cancel; enforces "
                "ownership; persists atomically.",
                "MenuService, HubLoadService, PricingService, OrderLifecycle, PaymentSelector, DbContext"],
               ["PricingService (524H0072)",
                "Validates subtotal, distance, load, line and quantity rules; calculates S/Q/V/F/D/R/RainFee once; "
                "returns an immutable breakdown.",
                "PricingInput, VoucherEvaluator, PriceBreakdown"],
               ["PaymentSelector + IPaymentStrategy (524H0072)",
                "Rejects an actor/method mismatch; selects the registered strategy; exposes a stable charge/refund "
                "contract with a correlation result.",
                "Five strategies, PaymentRecord, PaymentResult"],
               ["Order and PriceSnapshot (524H0072)",
                "Hold lines, totals, payment status and the immutable placement context (time, load, rain); expose "
                "state changes only through the transition service.",
                "OrderLine, PaymentRecord, StatusHistory"],
               ["OrderTransitionService (team / HT03)",
                "Applies the eight BR15 guards, appends audit entries and timestamps, and rejects invalid transitions.",
                "Order, StatusHistory, ActorContext"],
               ["MenuService (524H0189)",
                "Brand and menu CRUD, availability, hub-brand validation and soft deletion; preserves historical "
                "references.",
                "Brand, MenuItem, HubBrand, DbContext"],
               ["HubService (524H0188)",
                "Hub CRUD, positive integer capacity, hub-brand membership and rain setting.",
                "Hub, HubBrand, HubLoadService"],
               ["HubLoadService (524H0188)",
                "Derives active load from Confirmed/Preparing orders; reused by pricing, acceptance and R2.",
                "Order query, Hub"],
               ["VoucherService (support)",
                "Voucher CRUD and validation; retains referenced history; one voucher per order.",
                "Voucher, VoucherEvaluator"],
               ["RevenueReportService / HubReportService (support)",
                "R1 revenue by category and method, excluding Cancelled/Failed; R2 handled count, preparation mean, "
                "current load and Quiet Kitchen counts.",
                "Delivered orders, PriceSnapshot, StatusHistory"]],
              [4.1, 8.4, 4.0], font_size=8.5)

    doc.add_heading("2.4.2 UML Class Diagram / ERD", level=2)
    para(doc, "The class model shows the payment Strategy pattern and the Customer generalization. "
              "IPaymentStrategy defines charge and refund; MoMoStrategy, VNPayStrategy, ApplePayStrategy, "
              "PayPalStrategy and CodStrategy implement it. PaymentSelector chooses a strategy from the authenticated "
              "customer type and the selected method, so a new method is added by registering a class rather than "
              "editing conditional logic in OrderService.")
    add_image(doc, DIAG / "domain-and-payment.png", 15.8,
              "Figure 6 — Domain model and Payment Strategy (author: 524H0072, reviewed with 524H0189 and 524H0188).")
    add_image(doc, DIAG / "database-erd.png", 15.2,
              "Figure 7 — Shared-schema ERD (14 tables): users/profiles, hubs, brands, menu items, vouchers, system settings, orders, payments and history (team: 524H0072, 524H0189, 524H0188).")
    para(doc, "The ERD keeps one Users table with LocalProfile, TouristProfile and StaffProfile because the "
              "specification requires two customer types with different registration data but shared authentication. "
              "SystemSettings stores the global heavy-rain flag (BR5/FR16). OrderLines store the item name, unit price "
              "and line total at placement, and PriceSnapshot stores the placement time, hub load and rain flag, so "
              "historical orders stay reproducible after catalog or capacity changes.")

    doc.add_heading("2.4.3 Database Design Notes", level=2)
    bullet(doc, "money is stored as SQL decimal(19,4) (C# decimal) for intermediate calculations and all rounding "
                "happens once on the final total, half-up to 1,000 VND (BR11); stored line totals are not re-rounded "
                "individually.")
    bullet(doc, "Order, OrderLines, PriceSnapshot, PaymentRecord and the first StatusHistory row are written in one "
                "transaction (NFR05); a failure leaves no partial order.")
    bullet(doc, "rowversion columns protect Order, MenuItem and Hub writes from lost updates; a mismatch is rejected "
                "and reloaded rather than overwritten.")
    bullet(doc, "brands, menu items and hubs use soft deletion because orders, payments and reports must keep valid "
                "references; no cascade delete is allowed on financial data.")
    bullet(doc, "hub load is derived from Confirmed and Preparing orders divided by capacity; it is never stored as a "
                "mutable counter, which avoids drift after cancellation or capacity changes.")
    bullet(doc, "QR/online payments are simulated adapters: PaymentRecord stores method, amount, result and "
                "correlation id so R1 can reconcile revenue by method.")


def section_2_5(doc: Document):
    doc.add_heading("2.5 Architecture & Quality Design", level=1)
    add_image(doc, DIAG / "architecture.png", 15.8,
              "Figure 8 — Target MVC architecture and layer dependencies (team: 524H0072, 524H0189, 524H0188).")
    add_table(doc,
              ["Project", "Responsibility", "References"],
              [["SaigonBite.Domain", "Entities, value objects, lifecycle enums and contracts", "None"],
               ["SaigonBite.Application", "Order, menu, hub, voucher, transition and report services; pricing and "
                                         "payment selection", "Domain"],
               ["SaigonBite.Infrastructure", "EF6 mappings, migrations, persistence and simulated payment adapters",
                "Domain, Application"],
               ["SaigonBite.Web", "MVC controllers, ViewModels, Razor views, Bootstrap, EN/VI resources, composition",
                "Application, Infrastructure"],
               ["SaigonBite.Tests", "Unit and integration checks including EP/BVA and authorization", "Tested projects"]],
              [4.1, 9.6, 2.8], font_size=8.5)
    para(doc, "Design principles applied to the non-functional requirements:")
    add_table(doc,
              ["Principle", "How the design applies it", "Quality goal"],
              [["Separation of concerns", "Views display; controllers bind and authorize; services enforce business "
                                          "rules; EF maps persistence.", "NFR02, NFR04"],
               ["High cohesion, low coupling", "Pricing has no HTTP, EF or payment side effects; each service owns one "
                                               "business responsibility.", "NFR01, NFR02"],
               ["Single Responsibility", "Pricing, payment selection, state transitions and reports are separate "
                                         "classes.", "NFR02"],
               ["Open/Closed and Dependency Inversion", "OrderService depends on IPaymentStrategy through "
                                                        "PaymentSelector; a new method is a new class plus a "
                                                        "registration.", "NFR02"],
               ["Encapsulation and immutability", "State changes pass through OrderTransitionService; snapshots are "
                                                  "immutable after placement.", "NFR05, NFR06"],
               ["Server-side validation", "Role, ownership, state and numeric boundaries are rechecked in services, "
                                          "whatever the UI shows.", "NFR04"]],
              [4.4, 9.5, 2.6], font_size=8.5)
    para(doc, "Where the pricing rules live. BR1–BR14 are implemented in PricingService in the business layer, not in "
              "controllers or views, because every caller (web checkout, edit recalculation and tests) must obtain the "
              "same result and the rules must be testable without a browser. PricingService receives an authoritative "
              "input built from the selected items and the stored placement context, applies the calculation order S → Q "
              "→ V → F → D → R → RainFee and returns a breakdown without touching the database or any payment provider. "
              "Quiet Kitchen (load < 30%) and hub rejection (load ≥ 90%) are decided from HubLoadService so checkout, "
              "pricing and R2 share one definition of load.")
    para(doc, "NFR verification is planned for Part 3: NFR01 by timing the business layer on 1, 15 and 30-line orders, "
              "NFR02 by change-impact review of a new payment method, NFR03 by responsive and EN/VI checks, NFR04 by "
              "authorization tests, NFR05 by transaction failure tests and NFR06 by auditing every transition. No "
              "execution result is claimed in this design document.")


def section_2_6(doc: Document):
    doc.add_heading("2.6 UI/UX Design", level=1)
    para(doc, "The interactive prototype follows the four required stages — Sketch, Wireframe, Mockup and Prototype — "
              "and uses the SaigonBite V2 visual system: warm amber surfaces, brick-red primary actions, Vietnamese food "
              "imagery, bilingual VI/EN switching (NFR03) and a responsive Bootstrap 12-column grid layout (Bootstrap "
              "container/row/col breakpoints adapting between 1440 px desktop two-column layouts such as col-lg-7/col-lg-5 "
              "and 375–390 px single-column mobile views). The V2 prototype covers customer ordering for both Local "
              "Customers and Foreign Tourists, order management, menu and hub administration, and reports R1 and R2.")

    p = doc.add_paragraph()
    r = p.add_run("Figma V2 prototype link (page 04 Prototype → Present): ")
    r.bold = True
    r.font.size = Pt(10.5)
    add_hyperlink(p, FIGMA_PROTO, FIGMA_PROTO, size=10.5)

    p2 = doc.add_paragraph()
    p2.paragraph_format.space_after = Pt(4)
    r2 = p2.add_run("Figma design file (editor view): ")
    r2.italic = True
    r2.font.size = Pt(9)
    add_hyperlink(p2, FIGMA_FILE, FIGMA_FILE, size=9)

    add_table(doc,
              ["Stage", "What it shows", "Evidence"],
              [["Sketch", "Customer states and actions for the order flow before visual design.",
                "UC01 sketch, 15/11"],
               ["Wireframe", "Placement of status, order details and primary actions.",
                "UC01 wireframe, 15/11"],
               ["Mockup", "V2 visual design applied to Confirmed desktop and Placed mobile.",
                "Confirmed desktop, mobile 390 px"],
               ["Prototype", "V2 clickable paths, error branches and the public share link.",
                "91 V2 frames, 8 V2 flows"]],
              [2.6, 10.2, 3.7], font_size=8.5)

    sketch = svg_to_png(UC01IMG / "uc01-sketch-15.svg", BUILD / "sketch-15.png")
    wire = svg_to_png(UC01IMG / "uc01-wireframe-15.svg", BUILD / "wireframe-15.png")
    add_image_grid(doc, [
        (sketch, "Figure 9 — Sketch stage (UC01, 524H0072)."),
        (wire, "Figure 10 — Wireframe stage (UC01, 524H0072)."),
        (STAGES / "03-Mockup-Desktop.png", "Figure 11 — Mockup: V2 order tracking on desktop (524H0072)."),
        (STAGES / "03-Mockup-Mobile.png", "Figure 12 — Mockup: V2 order tracking on mobile 390 px (524H0072)."),
    ])

    doc.add_heading("Key screens (SaigonBite V2)", level=2)
    add_image_grid(doc, [
        (PREV / "trang-chu-desktop.png", "Figure 13 — V2 customer home and hub brand browsing, desktop (524H0072)."),
        (PREV / "trang-chu-mobile.png", "Figure 14 — V2 customer home, responsive mobile 375–390 px (524H0072)."),
        (PREV / "thanh-toan-desktop.png", "Figure 15 — V2 Local Customer checkout: MoMo, VNPay, COD & price breakdown (FR03/BR13, 524H0072)."),
        (PREV / "thanh-toan-tourist-desktop.png", "Figure 16 — V2 Foreign Tourist checkout (EN): Apple Pay, PayPal, COD & price breakdown (FR03/BR13/NFR03, 524H0072)."),
        (PREV / "uc01-theo-doi-vnpay.png", "Figure 17 — V2 Local Customer order tracking after VNPay payment (524H0072)."),
        (PREV / "uc01-theo-doi-apple-pay.png", "Figure 18 — V2 Foreign Tourist order tracking (EN) after Apple Pay (524H0072)."),
        (PREV / "uc01-kiem-tra-tinh-huong.png", "Figure 19 — V2 order lifecycle states and error scenarios: hub load ≥ 90%, COD cap, payment failure (524H0072)."),
        (PREV / "uc01-mobile-huy-375.png", "Figure 20 — V2 mobile cancel flow with simulated refund result (FR07/BR16, 524H0072)."),
        (PREV / "uc02-thuc-don-v2.png", "Figure 21 — V2 UC02 menu and brand administration (524H0189)."),
        (PREV / "uc03-cong-suat-v2.png", "Figure 22 — V2 UC03 hub capacity, rain flag and derived load (524H0188)."),
        (PREV / "r1-doanh-thu-v2.png", "Figure 23 — V2 R1 revenue report by category and all five payment methods (Team)."),
        (PREV / "r2-hieu-suat-v2.png", "Figure 24 — V2 R2 hub utilization, prep time and Quiet Kitchen report (524H0188)."),
    ])

    doc.add_heading("Interaction coverage", level=2)
    bullet(doc, "Checkout enforces actor-specific payment methods (MoMo, VNPay, COD for Local Customers; Apple Pay, "
                "PayPal, COD for Foreign Tourists) and the 1,000,000 VND COD cap before order placement (BR13/BR14).")
    bullet(doc, "Edit is offered only in Placed, cancel only in Placed or Confirmed, and tracking without actions from "
                "Preparing onward as Kitchen Staff and Shipper advance order states via HT03 (BR15–BR17).")
    bullet(doc, "Error branches cover hub load ≥ 90%, COD above 1,000,000 VND and a failed simulated payment.")
    bullet(doc, "Bootstrap responsive grid layouts were verified at 1440 px desktop and 375–390 px mobile with VI/EN "
                "language toggle support; mobile action targets are at least 44 × 44 px (NFR03).")
    bullet(doc, "Page 04 Prototype in the published Figma file contains 91 V2 screens across 8 V2 interactive flows "
                "(Local desktop, Mobile 375–390 px, Tourist checkout, Admin dashboard, UC02, UC03, R1 and R2).")


def references(doc: Document):
    doc.add_heading("References", level=1)
    refs = [
        ("SE_FinalProject_Requirements_SaigonBite_Sem01_2627.pdf. Specification version 1.0: BR1–BR18, technology "
         "tiers, use cases A–F and deliverables.", None),
        ("SE_FP_SaigonBite_StudentSurvivalGuide_Sem01_2627.pdf. Part 2 deliverables and Design Review guidance.", None),
        ("SE_FP_SaigonBite_CaseStudy_Sem01_2627.pdf. Business context and NFR baselines.", None),
        ("SE_Sem01_2627_Report_Template.docx. Official report structure, sections 2.1–2.6.", None),
        ("SaigonBite Team 3 Figma V2 prototype: ", FIGMA_PROTO),
        ("SaigonBite Team 3 Figma design file: ", FIGMA_FILE),
        ("SaigonBite Team 3 repository: ", GITHUB),
    ]
    for i, (text, url) in enumerate(refs, 1):
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Cm(0.8)
        p.paragraph_format.first_line_indent = Cm(-0.8)
        p.paragraph_format.space_after = Pt(2)
        r = p.add_run(f"[{i}]  ")
        r.font.size = Pt(9.5)
        r = p.add_run(text)
        r.font.size = Pt(9.5)
        if url:
            add_hyperlink(p, url, url, size=9.5)


def appendices(doc: Document):
    h = doc.add_heading("Appendix A: Contribution Statement", level=1)
    h.paragraph_format.page_break_before = True
    add_table(doc,
              ["Member", "Student ID", "Use case", "Contribution to Part 2"],
              [["Nguyễn Hải Triều", "524H0072", "UC01 Place and Manage Orders",
                "UC01 specification, activity diagrams, order state diagram, pricing and Payment Strategy design, "
                "checkout and order-management prototype, report assembly."],
               ["Dương Phạm Thái", "524H0189", "UC02 Manage Menu and Brands",
                "UC02 specification, activity diagram, catalog CRC responsibilities, menu/brand prototype, review of "
                "shared class model."],
               ["Lê Quang Tấn", "524H0188", "UC03 Manage Hubs and Capacity",
                "UC03 specification, activity diagram, hub/load design, ERD and R2 report, capacity prototype."]],
              [3.6, 2.4, 4.6, 5.9], font_size=8.5)

    doc.add_heading("Appendix B: Generative AI Usage Log", level=1)
    para(doc, "Per Section 7 of the course specification, all generative AI assistance used for brainstorming, "
              "structuring and review is declared below. Team members verified every diagram, business rule and "
              "requirement against the specification and retain full prompt records on request.")
    add_table(doc,
              ["Tool and date", "Purpose", "Prompt (summary)", "What you kept or changed", "How you verified it"],
              [["ChatGPT, 30 Oct 2026",
                "Interpret BR1–BR18 and check FR/BR mapping for UC01–UC03.",
                "\"Map BR1–BR18 to UC01, UC02 and UC03 pre/post-conditions and highlight ambiguities.\"",
                "Kept BR IDs from spec; rewrote flows and recorded AMB01–AMB06 as team assumptions.",
                "Cross-checked every rule and boundary against Specification Section 2B–C."],
               ["ChatGPT / Codex, 10–11 Nov 2026",
                "Draft PlantUML source for Use Case, Activity, State, Class/Strategy and ERD diagrams.",
                "\"Generate PlantUML diagrams for SaigonBite Payment Strategy, 8-state order lifecycle and single-schema ERD.\"",
                "Added SystemSettings table, aligned actor guards with BR15 and verified all student IDs.",
                "Rendered locally with PlantUML and checked against Section 2C state table and CRC cards."],
               ["ChatGPT / Codex, 14–19 Nov 2026",
                "Structure V2 Figma screens, responsive layout QA and English wording refinement.",
                "\"Audit SaigonBite V2 checkout, tracking and R1/R2 screens against Part 2 checklist and refine English report prose.\"",
                "Retained V2 orange theme only; added Foreign Tourist checkout (Apple Pay/PayPal) and fixed formatting.",
                "Tested public Figma link unauthenticated and ran automated deliverable checklist."]],
              [2.6, 2.9, 3.8, 3.8, 3.4], font_size=8.0)

    doc.add_heading("Appendix C: Detailed UC01 Activity Diagrams", level=1)
    para(doc, "UC01 was modelled at two levels. Section 2.3.1 contains the readable overview used for review; the four "
              "CRUD branches were also drawn separately at full page height. Because those vertical diagrams become "
              "illegible when scaled to A4, the table records where they are stored instead of embedding unreadable "
              "copies.")
    add_table(doc,
              ["Diagram", "Focus", "Source file"],
              [["Create order", "Hub and menu checks, pricing, payment branches, atomic save",
                "docs/part2/diagrams/uc01-create-activity.svg"],
               ["Read and track", "Ownership check, stored breakdown, status history",
                "docs/part2/diagrams/uc01-read-activity.svg"],
               ["Edit while Placed", "Snapshot reuse, price adjustment, concurrency guard",
                "docs/part2/diagrams/uc01-edit-activity.svg"],
               ["Cancel", "Placed/Confirmed guard, simulated refund, retained history",
                "docs/part2/diagrams/uc01-cancel-activity.svg"]],
              [3.4, 7.6, 5.5], font_size=8.5)
    para(doc, "The four SVG/PNG pairs are kept in the project repository (docs/part2/diagrams/) and in the team Drive "
              "folder together with this report, so the diagrams can be opened at full resolution during the Design "
              "Review.")


# --------------------------------------------------------------------------- build
def build_docx() -> Path:
    BUILD.mkdir(parents=True, exist_ok=True)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    if not TEMPLATE.exists():
        raise SystemExit(f"Template not found: {TEMPLATE}")
    doc = Document(str(TEMPLATE))
    clear_body(doc)
    setup_header_footer(doc)
    cover(doc)
    front_matter(doc)
    part2_intro(doc)
    section_2_1(doc)
    section_2_2(doc)
    section_2_3(doc)
    section_2_4(doc)
    section_2_5(doc)
    section_2_6(doc)
    references(doc)
    appendices(doc)
    for p in doc.paragraphs:
        if p.style is not None and p.style.name.startswith("Heading"):
            p.paragraph_format.keep_with_next = True
    doc.save(str(DOCX))
    return DOCX


def export_pdf(docx_path: Path, pdf_path: Path) -> None:
    import subprocess

    helper = ROOT / "scripts" / "export-docx-pdf.ps1"
    subprocess.run(
        ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(helper),
         "-Docx", str(docx_path), "-Pdf", str(pdf_path)],
        check=True,
    )
    with fitz.open(pdf_path) as d:
        toc = d.get_toc()
        print("PDF written:", pdf_path, "| pages:", d.page_count, "| bookmarks:", len(toc))
        for level, title, page in toc[:8]:
            print(f"  {'.' * level} {title} -> {page}")


if __name__ == "__main__":
    p = build_docx()
    print("DOCX written:", p)
    if "--pdf" in sys.argv:
        export_pdf(p, PDF)
