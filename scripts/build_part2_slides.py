"""Build Team3_Part2_Presentation.pptx (Design Review, 7-10 minutes).

Every slide has speaker notes with the spoken script and a time budget.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image
from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.util import Inches, Pt, Emu

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "output" / "part2" / "Team3_Part2_Presentation.pptx"
DIAG = ROOT / "docs" / "part2" / "diagrams"
PREV = ROOT / "outputs" / "saigonbite-v2" / "previews"
LOGO = ROOT / "outputs" / "saigonbite-v2" / "assets" / "logo.png"

NAVY = RGBColor(0x1F, 0x3A, 0x5F)
AMBER = RGBColor(0xE8, 0x9B, 0x1C)
BRICK = RGBColor(0xB0, 0x3A, 0x2E)
CREAM = RGBColor(0xFF, 0xF7, 0xE8)
GREY = RGBColor(0x5A, 0x5A, 0x5A)
WHITE = RGBColor(0xFF, 0xFF, 0xFF)
LIGHT = RGBColor(0xF2, 0xF5, 0xF9)

SW, SH = Inches(13.333), Inches(7.5)


def new(prs):
    return prs.slides.add_slide(prs.slide_layouts[6])


def rect(slide, x, y, w, h, color, line=None):
    s = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, x, y, w, h)
    s.fill.solid()
    s.fill.fore_color.rgb = color
    if line is None:
        s.line.fill.background()
    else:
        s.line.color.rgb = line
    s.shadow.inherit = False
    return s


def text(slide, x, y, w, h, value, size=16, bold=False, color=NAVY, align=PP_ALIGN.LEFT,
         anchor=MSO_ANCHOR.TOP, italic=False, space_after=4):
    tb = slide.shapes.add_textbox(x, y, w, h)
    tf = tb.text_frame
    tf.word_wrap = True
    tf.vertical_anchor = anchor
    tf.margin_left = tf.margin_right = Inches(0.02)
    tf.margin_top = tf.margin_bottom = 0
    lines = value if isinstance(value, list) else [value]
    for i, line in enumerate(lines):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.alignment = align
        p.space_after = Pt(space_after)
        r = p.add_run()
        r.text = line
        r.font.size = Pt(size)
        r.font.bold = bold
        r.font.italic = italic
        r.font.color.rgb = color
        r.font.name = "Calibri"
    return tb


def slide_title(slide, title, kicker=None, owner=None):
    rect(slide, 0, 0, SW, Inches(0.12), AMBER)
    y = Inches(0.38)
    if kicker:
        text(slide, Inches(0.6), y, Inches(9.5), Inches(0.3), kicker.upper(), size=11, bold=True, color=AMBER)
        y = Inches(0.68)
    text(slide, Inches(0.6), y, Inches(10.6), Inches(0.8), title, size=27, bold=True, color=NAVY)
    if owner:
        text(slide, Inches(10.9), y + Inches(0.12), Inches(1.9), Inches(0.4), owner, size=10, color=GREY,
             align=PP_ALIGN.RIGHT)
    rect(slide, Inches(0.6), Inches(1.52), Inches(1.1), Inches(0.045), BRICK)


def footer(slide, idx, total=12):
    rect(slide, 0, SH - Inches(0.42), SW, Inches(0.02), RGBColor(0xE0, 0xE4, 0xEA))
    text(slide, Inches(0.6), SH - Inches(0.36), Inches(8), Inches(0.25),
         "SaigonBite  •  Team 3  •  Part 2 Design Review", size=9, color=GREY)
    text(slide, Inches(11.4), SH - Inches(0.36), Inches(1.3), Inches(0.25), f"{idx} / {total}", size=9,
         color=GREY, align=PP_ALIGN.RIGHT)


def bullets(slide, items, x, y, w, h, size=15, color=RGBColor(0x33, 0x33, 0x33), gap=8):
    tb = slide.shapes.add_textbox(x, y, w, h)
    tf = tb.text_frame
    tf.word_wrap = True
    tf.margin_left = tf.margin_right = 0
    for i, item in enumerate(items):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.space_after = Pt(gap)
        p.line_spacing = 1.05
        r = p.add_run()
        r.text = "•  " + item
        r.font.size = Pt(size)
        r.font.color.rgb = color
        r.font.name = "Calibri"
    return tb


def picture(slide, path: Path, x, y, w, h, border=False):
    """Fit the picture inside the box, centred."""
    if not path.exists():
        text(slide, x, y, w, h, f"[missing {path.name}]", size=12, color=BRICK)
        return
    with Image.open(path) as im:
        iw, ih = im.size
    scale = min(w / iw, h / ih)
    nw, nh = int(iw * scale), int(ih * scale)
    px = x + (w - nw) // 2
    py = y + (h - nh) // 2
    pic = slide.shapes.add_picture(str(path), px, py, nw, nh)
    if border:
        pic.line.color.rgb = RGBColor(0xD5, 0xDB, 0xE3)
        pic.line.width = Pt(0.75)


def notes(slide, script: str, timing: str):
    slide.notes_slide.notes_text_frame.text = f"[{timing}]\n{script}"


def title_slide(prs):
    s = new(prs)
    rect(s, 0, 0, SW, SH, NAVY)
    rect(s, 0, Inches(2.05), SW, Inches(0.06), AMBER)
    text(s, Inches(0.75), Inches(2.5), Inches(11.8), Inches(0.6), "SAIGONBITE", size=54, bold=True, color=WHITE)
    text(s, Inches(0.75), Inches(3.45), Inches(11.8), Inches(0.5),
         "Part 2 — Software Design, Architecture & UI/UX", size=26, bold=True, color=RGBColor(0xFF, 0xC7, 0x78))
    text(s, Inches(0.75), Inches(4.15), Inches(11.8), Inches(0.4),
         "Design Review  •  17 November 2026  •  Team 3  •  Tier 3", size=15,
         color=RGBColor(0xD9, 0xE2, 0xEE))
    text(s, Inches(0.75), Inches(5.0), Inches(11.8), Inches(0.9),
         ["Nguyễn Hải Triều — 524H0072 — UC01",
          "Dương Phạm Thái — 524H0189 — UC02",
          "Lê Quang Tấn — 524H0188 — UC03"], size=14, color=WHITE, space_after=2)
    notes(s, "Good morning. We are Team 3 and this is our Part 2 design review for SaigonBite. "
             "I am Triều and I own UC01; Thái owns UC02 and Tấn owns UC03. We will explain the problem, "
             "the shared design, and then each use case with its evidence in Figma.", "0:00–0:30")


def agenda_slide(prs, idx):
    s = new(prs)
    slide_title(s, "What we will cover", "Agenda", "7–10 minutes")
    items = [
        ("1", "Problem and scope", "Triều · 0:30"),
        ("2", "Overall use cases and the order lifecycle", "Triều · 2:30"),
        ("3", "UC01 — ordering, pricing and payment", "Triều · 4:00"),
        ("4", "UC02 — menu and brands", "Thái · 6:00"),
        ("5", "UC03 — hubs, capacity and data model", "Tấn · 7:30"),
        ("6", "UI/UX evidence, quality and next steps", "Team · 9:00"),
    ]
    y = Inches(1.95)
    for num, label, who in items:
        rect(s, Inches(0.7), y, Inches(0.55), Inches(0.55), CREAM)
        text(s, Inches(0.7), y + Inches(0.09), Inches(0.55), Inches(0.4), num, size=18, bold=True,
             color=BRICK, align=PP_ALIGN.CENTER)
        text(s, Inches(1.45), y + Inches(0.09), Inches(7.4), Inches(0.4), label, size=17, color=NAVY)
        text(s, Inches(9.0), y + Inches(0.11), Inches(3.4), Inches(0.35), who, size=13, color=GREY,
             align=PP_ALIGN.RIGHT)
        y += Inches(0.72)
    footer(s, idx)
    notes(s, "The review is structured in six short parts. We will keep to eight minutes of speaking and "
             "leave time for questions. Each member presents the use case they own.", "0:30–0:50")


def problem_slide(prs, idx):
    s = new(prs)
    slide_title(s, "The problem we designed for", "Context", "524H0072")
    bullets(s, [
        "Delivery-only hubs host several brands but share one finite kitchen capacity.",
        "Demand is uneven: one hub overloads while another still has spare capacity.",
        "Customers must see the true total before ordering, then track, edit or cancel by state.",
        "Local customers and foreign tourists differ in language and payment methods (BR13).",
        "Scope of this review: UC01, UC02, UC03, the HT03 lifecycle and reports R1/R2.",
    ], Inches(0.65), Inches(1.85), Inches(7.3), Inches(4.6), size=16, gap=12)
    rect(s, Inches(8.35), Inches(1.85), Inches(4.35), Inches(4.55), CREAM)
    text(s, Inches(8.7), Inches(2.05), Inches(3.7), Inches(0.35), "Design constraints", size=13, bold=True,
         color=BRICK)
    text(s, Inches(8.7), Inches(2.5), Inches(3.7), Inches(3.7), [
        "Tier 3 stack: ASP.NET MVC, EF6 Code First, SQL Server, Bootstrap.",
        "Payments are simulated; no live money moves.",
        "Business rules BR1–BR18 keep the specification's identifiers.",
        "Nothing in this deck claims implementation results.",
    ], size=13, color=GREY, space_after=10)
    footer(s, idx)
    notes(s, "SaigonBite is not a normal food-delivery shop. One kitchen serves several brands, and its capacity is "
             "the real bottleneck. Our design has to keep three things true at once: the price is reproducible, a "
             "hub above 90 percent load refuses new orders, and every state change is auditable. We designed against "
             "Tier 3, so payments are simulated rather than integrated.", "0:50–1:40")


def usecase_slide(prs, idx):
    s = new(prs)
    slide_title(s, "Overall use cases and actor generalization", "2.1 Design", "Team")
    picture(s, DIAG / "overall-use-case.png", Inches(0.55), Inches(1.75), Inches(5.5), Inches(5.25), border=True)
    bullets(s, [
        "Customer is abstract; Local Customer and Foreign Tourist specialize it.",
        "The specialization carries the payment difference (BR13) without duplicating UC01.",
        "UC01 = create, read/track, edit, cancel.",
        "UC02 = brands and menu; UC03 = hubs, capacity, membership.",
        "HT01–HT04 are supporting functions required by the specification.",
    ], Inches(6.45), Inches(1.95), Inches(6.2), Inches(4.8), size=15, gap=12)
    footer(s, idx)
    notes(s, "This is the shared picture. Notice that the customer actor is abstract, and the two customer types "
             "inherit from it. That single decision keeps one ordering use case while still allowing different "
             "registration data, language and payment methods. Everything else — kitchen, shipper, admin — stays a "
             "separate actor because permissions differ.", "1:40–2:30")


def uc01_slide(prs, idx):
    s = new(prs)
    slide_title(s, "UC01 — place, track, edit and cancel", "2.2–2.3 Customer flow", "524H0072")
    picture(s, DIAG / "uc01-order-activity.png", Inches(0.55), Inches(1.8), Inches(8.3), Inches(4.9), border=True)
    bullets(s, [
        "One entry point, four CRUD branches.",
        "Create: hub, menu and load checks before pricing.",
        "Edit: only in Placed, reuses the placement snapshot.",
        "Cancel: Placed or Confirmed, full simulated refund.",
        "Read: ownership checked on every request.",
    ], Inches(9.1), Inches(2.0), Inches(3.9), Inches(4.2), size=13, gap=10)
    footer(s, idx)
    notes(s, "UC01 is my use case. The customer has one entry point but four behaviours. On create, the system "
             "validates the hub, the menu and the load before it calculates anything. Edit is allowed only while the "
             "order is Placed, and it must reuse the original time, load and rain snapshot — otherwise the customer "
             "could get a different price just because the hub became busy. Cancel is allowed in Placed or "
             "Confirmed, and an online payment is fully refunded in simulation. The order is never hard-deleted.",
          "2:30–4:00")


def pricing_slide(prs, idx):
    s = new(prs)
    slide_title(s, "Pricing rules and the payment Strategy", "2.4 Static design", "524H0072")
    picture(s, DIAG / "domain-and-payment.png", Inches(0.5), Inches(1.75), Inches(7.6), Inches(3.6), border=True)
    rect(s, Inches(8.35), Inches(1.75), Inches(4.4), Inches(3.55), CREAM)
    text(s, Inches(8.65), Inches(1.9), Inches(3.8), Inches(0.35), "Worked example (BR3–BR11)", size=12, bold=True,
         color=BRICK)
    text(s, Inches(8.65), Inches(2.28), Inches(3.8), Inches(2.9), [
        "Food subtotal: 200,000 VND",
        "Load 20% → Quiet Kitchen −10%: −20,000",
        "Voucher: 0",
        "Delivery fee: +25,000",
        "Surcharges: 0",
        "Total: 205,000 VND",
    ], size=13, color=GREY, space_after=4)
    bullets(s, [
        "Pricing lives in PricingService, not in controllers or views: same rules for checkout, edit and tests.",
        "Order is S → Q → V → F → D → R → RainFee, rounded once at the end.",
        "IPaymentStrategy + five strategies; PaymentSelector rejects actor/method mismatches.",
        "Adding a method = one new class plus a registration, no edit to OrderService.",
    ], Inches(0.65), Inches(5.5), Inches(12.1), Inches(1.6), size=13, gap=4)
    footer(s, idx)
    notes(s, "Two design decisions matter here. First, all money rules live in PricingService in the business layer, "
             "because checkout, edit and automated tests must produce the same number. The calculation order is fixed "
             "and rounding happens once. Second, payment uses the Strategy pattern: one interface, five strategies, "
             "and a selector that enforces which methods each customer type may use. A new method is a new class, not "
             "another if-statement.", "4:00–5:10")


def lifecycle_slide(prs, idx):
    s = new(prs)
    slide_title(s, "One lifecycle, eight states, no shortcuts", "2.3.4 Shared state diagram", "Team")
    picture(s, DIAG / "order-state.png", Inches(0.7), Inches(1.7), Inches(4.6), Inches(5.4), border=True)
    bullets(s, [
        "Placed → Confirmed → Preparing → Ready → Out for Delivery → Delivered.",
        "Cancelled and Failed are terminal branches; so is Delivered.",
        "Guards name the actor: own hub, assigned shipper, own order.",
        "Preparing → Cancelled and Ready → Delivered are invalid by design.",
        "Shipper assignment is AMB01, documented as an assumption — not a new state.",
    ], Inches(5.7), Inches(2.0), Inches(6.9), Inches(4.5), size=15, gap=12)
    footer(s, idx)
    notes(s, "The order lifecycle is a team model and it is deliberately strict. There are exactly eight states and "
             "eight permitted transitions, each with an actor and a guard. You cannot cancel once the kitchen has "
             "started preparing, and you cannot jump from Ready to Delivered. This matters because the reports and "
             "the load calculation both depend on these states.", "5:10–5:50")


def uc02_slide(prs, idx):
    s = new(prs)
    slide_title(s, "UC02 — menu and brand management", "2.2.2 / 2.3.2 Admin flow", "524H0189")
    picture(s, DIAG / "uc02-menu-activity.png", Inches(0.5), Inches(1.75), Inches(8.0), Inches(3.4), border=True)
    picture(s, PREV / "uc02-thuc-don-v2.png", Inches(8.75), Inches(1.8), Inches(4.0), Inches(3.3), border=True)
    bullets(s, [
        "Admin creates a brand, then items linked to that brand and its serving hubs.",
        "Price and availability changes apply to future orders; old order lines keep their snapshot price.",
        "Delete is a soft delete: historical orders and R1 revenue stay valid.",
        "Customers only see Available items served by the selected hub (BR12).",
    ], Inches(0.6), Inches(5.35), Inches(12.2), Inches(1.7), size=13, gap=4)
    footer(s, idx)
    notes(s, "I am Thái and I own UC02. The important idea is that catalog data is shared: UC01 consumes it, and R1 "
             "revenue depends on its categories. So we never hard-delete a brand or an item. A price change affects "
             "future orders only; an existing order line keeps the price stored when it was placed. The screen on the "
             "right is the actual V2 prototype for this flow.", "5:50–6:50")


def uc03_slide(prs, idx):
    s = new(prs)
    slide_title(s, "UC03 — hubs, capacity and derived load", "2.2.3 / 2.3.3 Admin flow", "524H0188")
    picture(s, DIAG / "uc03-hub-activity.png", Inches(0.5), Inches(1.75), Inches(8.0), Inches(3.4), border=True)
    picture(s, PREV / "uc03-cong-suat-v2.png", Inches(8.75), Inches(1.8), Inches(4.0), Inches(3.3), border=True)
    bullets(s, [
        "Capacity is a positive integer; load is always derived, never typed in.",
        "Load = Confirmed + Preparing orders ÷ capacity. 30% is not a discount; 90% refuses new orders (BR8).",
        "Lowering capacity can push load above 100%, but it never rewrites history.",
        "Deactivating a hub stops new orders and lets existing work finish.",
    ], Inches(0.6), Inches(5.35), Inches(12.2), Inches(1.7), size=13, gap=4)
    footer(s, idx)
    notes(s, "I am Tấn, UC03. The load percentage is the heart of the system: it decides Quiet Kitchen discounts, "
             "it decides whether a hub accepts an order, and R2 reports it. That is why load is derived from real "
             "orders instead of stored in a counter. If an admin lowers capacity, the percentage can exceed one "
             "hundred, which is displayed honestly, but past orders are untouched.", "6:50–7:40")


def data_slide(prs, idx):
    s = new(prs)
    slide_title(s, "Data model and MVC architecture", "2.4–2.5 Structure", "524H0188 / Team")
    picture(s, DIAG / "database-erd.png", Inches(0.4), Inches(1.7), Inches(6.4), Inches(3.3), border=True)
    picture(s, DIAG / "architecture.png", Inches(7.0), Inches(1.7), Inches(6.0), Inches(3.3), border=True)
    bullets(s, [
        "One Users table with Local, Tourist and Staff profiles: shared login, different data.",
        "Order lines store name, unit price and line total; PriceSnapshot stores time, load and rain.",
        "Order, lines, snapshot, payment and first history row commit in one transaction (NFR05).",
        "Pricing sits in the business layer; rowversion guards concurrent edits on Order, MenuItem and Hub.",
    ], Inches(0.6), Inches(5.25), Inches(12.2), Inches(1.8), size=13, gap=4)
    footer(s, idx)
    notes(s, "The ERD on the left keeps one Users table with three profiles, because authentication is shared. Order "
             "lines and the price snapshot are what make history reproducible. On the right is the MVC layering: "
             "controllers only bind and authorize, services own the rules, and EF is confined to infrastructure. "
             "The order, its lines, the snapshot, the payment and the first history row are one transaction.",
          "7:40–8:30")


def ui_slide(prs, idx):
    s = new(prs)
    slide_title(s, "From sketch to a clickable prototype", "2.6 UI/UX", "Team")
    shots = [
        (PREV / "trang-chu-desktop.png", "Home — desktop"),
        (PREV / "thanh-toan-desktop.png", "Checkout — price breakdown"),
        (PREV / "uc01-theo-doi-vnpay.png", "Tracking — VNPay"),
        (PREV / "uc01-mobile-huy-375.png", "Cancel — mobile 375 px"),
    ]
    x = Inches(0.55)
    for path, label in shots:
        picture(s, path, x, Inches(1.8), Inches(2.95), Inches(3.35), border=True)
        text(s, x, Inches(5.2), Inches(2.95), Inches(0.3), label, size=10, color=GREY, align=PP_ALIGN.CENTER)
        x += Inches(3.12)
    bullets(s, [
        "Four stages preserved: Sketch → Wireframe → Mockup → Prototype.",
        "107 frames, 772 links, 63 required routes; desktop and mobile verified.",
        "Inputs enforced in the prototype: hub load, COD cap, failed payment.",
        "Public prototype link is in readme.txt and report section 2.6.",
    ], Inches(0.6), Inches(5.6), Inches(12.2), Inches(1.3), size=13, gap=3)
    footer(s, idx)
    notes(s, "For UI/UX we kept the four required stages instead of only showing a polished mockup. The prototype "
             "contains 107 frames and 63 required routes, including the error branches: a hub at 90 percent, a COD "
             "order above one million, and a failed payment. The public link is in the readme and in section 2.6 of "
             "the report.", "8:30–9:10")


def quality_slide(prs, idx):
    s = new(prs)
    slide_title(s, "Quality goals and what comes next", "2.5 / closing", "Team")
    rows = [
        ("NFR01 Performance", "Pricing < 1.5 s for 30 lines", "Timed business-layer runs in Part 3"),
        ("NFR02 Maintainability", "New payment method = one class", "Change-impact review"),
        ("NFR03 Usability", "Responsive, EN/VI", "Desktop and 375 px evidence"),
        ("NFR04 Security", "Server-side role and ownership checks", "Authorization tests"),
        ("NFR05 Reliability", "Atomic order save", "Failure-injection tests"),
        ("NFR06 Traceability", "Every transition audited", "History checks"),
    ]
    y = Inches(1.85)
    for name, design, proof in rows:
        rect(s, Inches(0.6), y, Inches(12.1), Inches(0.62), LIGHT)
        text(s, Inches(0.8), y + Inches(0.13), Inches(3.0), Inches(0.35), name, size=12, bold=True, color=NAVY)
        text(s, Inches(3.9), y + Inches(0.13), Inches(4.4), Inches(0.35), design, size=12, color=GREY)
        text(s, Inches(8.5), y + Inches(0.13), Inches(4.0), Inches(0.35), proof, size=12, color=GREY)
        y += Inches(0.68)
    text(s, Inches(0.6), Inches(6.0), Inches(12.1), Inches(0.75),
         "Next: EF migration and the vertical order flow, then edit/cancel, kitchen and shipper, R1/R2, and the "
         "Part 3 test evidence.", size=14, bold=True, color=BRICK)
    footer(s, idx)
    notes(s, "We close with quality. Each non-functional requirement has a design decision and a planned check, but "
             "we are not claiming results yet — those belong to Part 3. The next step is the MVC application with the "
             "EF migration and one vertical order flow, then the reports and the test evidence. Thank you — we are "
             "happy to take questions.", "9:10–9:40")


def build():
    prs = Presentation()
    prs.slide_width, prs.slide_height = SW, SH
    title_slide(prs)
    agenda_slide(prs, 2)
    problem_slide(prs, 3)
    usecase_slide(prs, 4)
    uc01_slide(prs, 5)
    pricing_slide(prs, 6)
    lifecycle_slide(prs, 7)
    uc02_slide(prs, 8)
    uc03_slide(prs, 9)
    data_slide(prs, 10)
    ui_slide(prs, 11)
    quality_slide(prs, 12)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    prs.save(str(OUT))
    print("PPTX written:", OUT, "| slides:", len(prs.slides))


if __name__ == "__main__":
    build()
