# Grade estimates

The percentages below are internal evidence estimates, calculated separately for minimum and advanced rows in the [traceability matrix](requirements-traceability.md). One point for PASS and half for PARTIAL, divided by applicable requirements. FAIL and NOT VERIFIED contribute zero. NOT APPLICABLE is excluded. Decimal percentages are rounded to one decimal. The grade estimates and ranges reflect materiality, missing prerequisites and delivery evidence. They are not an institutional formula. The official thesis, scoring sheets and any exception approval were unavailable.

| Track | Minimum evidence | Advanced evidence | Advanced PASS | Mandatory blockers (count and IDs) | Estimated /10 | Plausible range | Confidence |
|---|---:|---:|---:|---|---:|---|---|
| Backend | 90.0% (17 + 2/2 of 20) | 37.5% (2 + 5/2 of 12) | 2 | 2 · B-M09, B-M16 | 4 | 4–6 | High |
| Frontend | 96.4% (26 + 2/2 of 28) | 83.3% (14 + 7/2 of 21) | 14 | 0 | 8 | 7–9 | High |
| Adaptive UI | 84.4% (11 + 5/2 of 16) | 65.0% (5 + 3/2 of 10) | 5 | 0 hard blockers; A-M07/A-M08 evidence incomplete | 7 | 6–8 | Medium-high |
| API Documentation | 77.8% (11 + 6/2 of 18) | 97.1% (16 + 1/2 of 17) | 16 | 2 · D-M05/D-M06 combined completeness gap, D-M16 thesis evidence | 6 | 5–8 | Medium-high |
| Database OLTP | 87.5% (15 + 5/2 of 20) | 50.0% (2 + 2/2 of 6) | 2 | 3 · DB-M16/17/18 actual application privileges | 7 | 5–8 | Medium |
| CI/CD | 81.3% (16 + 7/2 of 24) | 36.1% (4 + 5/2 of 18) | 4 | 3 · CI-M11, CI-M16, CI-M24 for latest candidate | 4 | 4–6 | High |
| Cloud | 38.9% (2 + 3/2 of 9) | 27.3% (0 + 6/2 of 11) | 0 | 4 · C-M01, C-M02, C-M04, C-M08 | 2 | 1–4 | High |
| Machine Learning | 23.5% (0 + 8/2 of 17) | 22.7% (0 + 5/2 of 11) | 0 | 9 · ML-M02/03/04/05/06/08/09/10/11 | 2 | 1–4 | High |

A blocker is a distinct gap with direct minimum-level impact, including proof required by the rubric. Related API example gaps are one blocker; missing deployment evidence is relevant separately under multiple tracks, without stacking duplicate penalties inside a grade. Advanced percentages are counts of evidence, not gates that override a failed minimum. ML is especially constrained: local-only third-party inference contributes partial evidence; published ML work is absent. CI points distinguish historic Docker execution from the newest red head.

## Paths to higher grades

| Track | Why current estimate is limited | To substantiate ~7 | To target 9–10 |
|---|---|---|---|
| Backend | Production accessibility is mandatory; latest main CI is red. | Verify a real HTTPS release, migration run and restart | Demonstrate registry digest, protected delivery, trace/log/alert evidence and repeatable rollback |
| Frontend | Two minimum rows have incomplete breadth; Lighthouse and accessibility sampling are narrow. | Keep Redux/browser regression checks on delivery SHA; complete contract excerpts | Demonstrate all advanced core directions together with mobile/desktop route metrics and assistive technology review |
| Adaptive UI | Physical display scales, system keyboards, full contrast and target coverage are incomplete. | Finish touch/contrast and keyboard checklist on complete key flows | Add real devices, 4K/ultrawide, mobile keyboards and audited adaptive states |
| API Documentation | Only a minority of request/success payloads have examples; thesis evidence is absent. | Add fixture-derived examples for key routes and submit written documentation strategy | Cover all body operations, describe data models, enforce schema validation for every fixture |
| Database OLTP | DDL and transactions are strong; actual runtime privileges and normalization rationale incomplete. | Run restricted API and separate migrations on target; submit grants and FD/3NF appendix | Record query plans and a justified masking/research projection; rehearse restore with measured RTO/RPO |
| CI/CD | No completed CD, and latest backend verify fails formatting before Docker. | Green exact release head, execute deployment, capture digest and health | Show both containerization and effective quality direction, add audit-grade rollback and health-gated migration |
| Cloud | No live deployment, public endpoint or managed service proof. | Provide accessible HTTPS backed by managed service, TLS/roles, and bounded costs | Show managed stack, controlled release, monitored restart/fault drill and reproducible infrastructure |
| Machine Learning | No trained project model, appropriate dataset, splits or model-quality measurements. | Agree an ethically labeled task/dataset and submit a genuine trained baseline with held-out results | Compare models, track experiments, cover training/inference, report uncertainty, measured performance and bias |

### Meaningful advanced directions

- Frontend direction 2 is substantive. Store, RTK Query, export Thunk and cancellable telemetry refresh Saga all have code and tests. Direction 1 is substantially evidenced by feature modularity and route chunks, with formal dependency rules partial. Direction 4 includes a real dashboard, custom design system, charts, themes and limited accessibility evidence. Direction 3 has code splitting and initial bundle measurements; authenticated Lighthouse breadth is incomplete. This supports a credible high-grade path. It does not guarantee a 9 on the current evidence.
- CI/CD direction 2 has a real historical multi-stage build and Compose execution. Direction 4 has passing historical gates, 80% line threshold and current failing gate enforcement. Required PR reviews or branch protection have not been demonstrated. Observability and controlled CD are partial; IaC is absent. The academic requirement of two meaningful advanced directions is contingent on the evaluator accepting the present quality-gate evidence and on first clearing the mandatory release gap.
- Database strengths are transaction boundaries, referential integrity and indexing for idempotency/ownership. PostgreSQL OLTP is appropriate. Views, triggers, routines or an additional DBMS require a technical reason and do not repair mandatory privilege evidence.
- See [supported penalty findings](critical-deficiencies.md#scoring-deductions) for potential rubric deductions. No deductions have been applied as though the official weight or institutional judgment were known.
