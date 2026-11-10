# Editable UML sources

Run `scripts/render-diagrams.ps1 -PlantUmlJar path/to/plantuml.jar` with Java and Graphviz installed. The initial renderer is PlantUML 1.2025.2 (Java 8 compatible), downloaded from Maven Central into ignored `.tools`; it is not part of the project source or application dependencies. Outputs SVG/PNG beside each source. All sources identify designated student owners and AI-assisted review status.

Do not mistake diagram syntax validation for application tests or instructor approval. Review exact BR15 guards, customer generalization, multiplicities and payment selection before incorporating figures into the official Part 2 report.

UC01 behavioral review (11 November work item, executed early on 10 November): `uc01-order-activity` is the overview. The added `uc01-create-activity`, `uc01-read-activity`, `uc01-edit-activity` and `uc01-cancel-activity` expand its CRUD branches. `order-state` remains the shared eight-state lifecycle, including HT03 actors. All six reviewed sources have SVG and PNG outputs. See [the review and traceability](../uc01/05-behavioral-review.md).
