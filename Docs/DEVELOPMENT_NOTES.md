# 🧠 Who Said Quiet Callouts – Development Notes  
**Version:** v0.9.1 Alpha (b Revision)  
**Date:** March 7 2026  
**Maintainer:** Who Said Quiet Team · Assisted by Galaxy AI (GPT‑5 Model by OpenAI)

---

## 📋 Project Overview
Who Said Quiet Callouts (WSQ) exists to deliver immersive, story‑driven law‑enforcement scenarios built on the LSPDFR framework.  
The development goal is to merge **realistic police response behavior**, **replayable AI variability**, and **plugin interoperability** without sacrificing performance.

---

## 🧱 Design Philosophy
| Core Principle | Description |
|:--|:--|
| **Narrative Immersion** | Each callout tells a story with context, emotion, and choice‑driven outcome. |
| **AI Autonomy** | Non‑player actors follow realistic decision trees (Compliant / Flee / Hostile). |
| **Code Clarity** | All classes should read like tutorial‑level examples; avoid obscure logic. |
| **Performance First** | Maintain ≤ 600 MB RAM footprint and ≤ 10 % CPU usage during runtime. |
| **Respect for Sensitive Topics** | Any content involving mental health or violence must include opt‑out options and clear player consent. |

---

## 🧩 Current Feature Set (v0.9.1 Alpha)
- **21 Callouts** (total including Suicide Attempt).  
- **Radiant AI Engine:** Behavior states selected by weighted random generator.  
- **Integration Harmony:** 9 supported LSPDFR plugins with conflict detection.  
- **Manual Callout Menu:** Callout Interface API integration (v0.9.0+).  
- **Variable Cooldown System:** Randomized delays (30 – 300 s).  
- **XML Doc Summaries:** IntelliSense documentation throughout codebase.  
- **QA Validation:** Memory leak and overflow free status verified.  

---

## ⚙️ Coding Standards
1. **Namespaces**
   - Use `WhoSaidQuietCallouts.Callouts` for scene classes.  
   - Use `WhoSaidQuietCallouts.Integration` for plugin wrappers.  
   - Use `WhoSaidQuietCallouts.Include` for core files and helpers.

2. **Formatting Rules**
   - Indent with 4 spaces (no tabs).  
   - One class per file.  
   - Namespace imports sorted alphabetically.  
   - Place XML `/// <summary>` docstrings above each public method.

3. **Behavior AI Implementation**
   - Enum `BehaviorState { Compliant, Flee, Hostile }` exists per callout.  
   - Use `IntegrationDelegator.RequestBackup();` to avoid hard‑coded plugin calls.  
   - Never instantiate `GameFiber.StartNew()` without explicit termination criteria.

4. **Logging Policy**
   - Use `WSQLogger.Info()` for routine operations.  
   - Use `WSQLogger.Warn()` for recoverable exceptions.  
   - Use `WSQLogger.Error()` only for critical failures (pre‑shutdown).  
   - Logs rotate by session under `Plugins/LSPDFR/Logs/WhoSaidQuietCallouts.txt`.

5. **INI Management**
   - Default keys must auto‑populate via `EnsureIniDefaults()`.  
   - Player custom values never overwritten unless missing.  
   - Future callouts added → append to `CalloutKeys[]`.

---

## 🧭 Roadmap / Future Versions
| Milestone | Planned Version | Description |
|:--|:--:|:--|
| **v2.0 – Optimization Build** | Q4 2026 | Refactor core for async fiber handling and AI pathfinding. |
| **v2.1 – Audio Narration Pack** | 2027 | Optional voice lines for dispatcher and peds. |
| **v2.2 – Dynamic Events Framework** | 2027 | Allow other plugins to trigger WSQ scenes via API. |
| **v3.0 – Community Edition** | TBD | Open contribution branch for third‑party callout submissions. |

---

## 🧰 Testing Checklist (Developers)
Before committing new callouts or features:
- [ ] Compile with zero warnings or errors.  
- [ ] Confirm INI auto‑generation and toggle visibility.  
- [ ] Run 5 in‑game tests (accepted / cancelled / force‑end / pursuit / hostile).  
- [ ] Review entity cleanup output (`SceneCleanup()` executions must succeed).  
- [ ] Add CHANGELOG and version increment.  
- [ ] Upload to GitHub branch with tag and note in `VERSION_HISTORY.md`.

---

## 🧩 Community Contributions
Developers may propose callouts through pull requests or issue submissions.  
Contribution requirements:
1. Use existing code patterns (`OnBeforeCalloutDisplayed()`, `OnCalloutAccepted()`, `Process()`, `End()`).  
2. Include XML docs and INI toggle entry.  
3. Provide a brief one‑line description for CHANGELOG.md.  
4. Must pass QA checklist before merge.

---

## ⚖️ Code Integrity Policy
- Main branch (`master`) only receives tested releases.  
- All experimental features go through `dev` branch pull requests.  
- Commits should follow semantic versioning (`vX.Y.Z`).  
- Each release requires a CHANGELOG entry and tag.

---

## 💬 Notes on Sensitive Content
The **Suicide Attempt** callout remains disabled by default.  
Developers must retain the content advisory section in README.md and the opt‑in INI toggle.  
All future sensitive scenarios must include a similar player consent measure.

---

## 🧾 Developer Contacts
- **dbrown1986:** Who Said Quiet Team (Lead Scripter / Coordinator)  
- **AI Assistant:** Galaxy AI ( GPT‑5 Model by OpenAI )  
- **Testing Coordinator:** Community Beta Group v2026  

---

### 🏁 End of File  
*(All development notes documented for GitHub Release v1.9.1 – March 7 2026)*
