from pathlib import Path
import csv,json
from docx import Document

root=Path(__file__).resolve().parents[1]
source=root/'tmp/final-review/reviewed-en.docx'
if not source.exists():source=root/'output/part1/SaigonBite_Part1_Tier3_OfficialTemplate_EN.docx'
doc=Document(source)
fr=next(t for t in doc.tables if len(t.columns)==5 and t.cell(0,0).text=='ID')
nfr=next(t for t in doc.tables if len(t.columns)==4 and t.cell(0,1).text=='Attribute')
amb=next(t for t in doc.tables if t.cell(0,1).text=='Ambiguity or gap')
rtm=next(t for t in doc.tables if len(t.columns)==6 and t.cell(0,0).text=='ID')
lines=['# Requirements Baseline','', 'Group 3; extracted from the reviewed Part 1 English draft on 30 October 2026. Requirement IDs are retained; instructor approval is not claimed. The source Word document and source PDFs remain local, outside the code repository.', '', '## Functional requirements','']
frmap={}
for r in fr.rows[1:]:
 cells=[c.text.replace('\n',' ') for c in r.cells];id,requirement,uc,br,reports=cells
 frmap[id]=cells
 lines.extend(['### '+id,requirement,'','Use case: '+uc+' | Rules/source: '+br+' | Report: '+reports,''])
lines.extend(['## Quality attributes',''])
for r in nfr.rows[1:]:
 id,kind,target,verify=[c.text.replace('\n',' ') for c in r.cells]
 lines.extend(['### '+id+' '+kind,target,'','Planned verification: '+verify,''])
lines.extend(['## Ambiguities and proposed assumptions',''])
for r in amb.rows[1:]:
 id,gap,proposal,answer=[c.text.replace('\n',' ') for c in r.cells]
 lines.extend(['### '+id,gap,'','Proposal: '+proposal,'','Instructor status: '+answer,''])
lines.extend(['## Source documents','','- SE_FinalProject_Requirements_SaigonBite_Sem01_2627.pdf','- SE_FP_SaigonBite_CaseStudy_Sem01_2627.pdf','- SE_FP_SaigonBite_StudentSurvivalGuide_Sem01_2627.pdf','- SE_Sem01_2627_Report_Template.docx','','Specification controls fixed BR values. AMB policies are team proposals until instructor answers are recorded.'])
(root/'docs/requirements-baseline.md').write_text('\n'.join(lines),encoding='utf-8')
with (root/'docs/part2/traceability.csv').open('w',encoding='utf-8',newline='') as f:
 writer=csv.writer(f);writer.writerow(['Requirement','UseCases','BR_or_source','Design','PlannedComponent','CheckGroup','ImplementationStatus'])
 for r in rtm.rows[1:]:
  id,uc,design,component,checks,verdict=[c.text.replace('\n',' ') for c in r.cells]
  refs=frmap.get(id,['','','','NFR baseline'])[3]
  designpath='docs/part2/use-cases.md' if id.startswith('FR') else 'docs/part2/architecture.md'
  status='Design drafted; implementation pending'
  if id in ['FR01','FR02','FR03','FR04']:status='Pure pricing/payment guards checked; full order integration pending'
  if id=='FR17':status='Pure transition guards checked; persistence/auth integration pending'
  writer.writerow([id,uc,refs,designpath+'; '+design,component,checks,status])
assert len(fr.rows)==23 and len(nfr.rows)==7 and len(amb.rows)==7 and len(rtm.rows)==29
print('Baseline extracted: 22 FR, 6 NFR, 6 AMB; RTM: 28 unique requirement rows.')
