# 🧾 Who Said Quiet Callouts – Version History  
**Project Timeline:** 2024 → 2026  
**Last Updated:** March 7 2026  

---

## 🌱 Version Index
| Version | Codename | Release Date | Major Focus |
|:--:|:--|:--:|:--|
| 0.1.0 | Project Foundation | July 30 2024 | Initial framework + 18 base callouts |
| 0.1.5 | Radiant AI Behavior System | Sept 28 2024 | Compliant / Flee / Hostile AI states |
| 0.2.0 | Narrative Enhancement Pack | Nov  5 2024 | Story depth + SuicideAttempt prototype |
| 0.3.0 | Stability & Cleanup Framework | Jan 15 2025 | SafeCleanup & OnDuty state hook |
| 0.4.0 | Logging & INI Upgrade | Mar  2 2025 | LogLevel support (0‑3) + INI auto‑repair |
| 0.5.0 | Integration Harmony Build | Jun 14 2025 | 9 plugin integrations + conflict system |
| 0.6.0 | Variable Dispatch Cooldown | Oct 11 2025 | Random interval (30‑300 s) dispatch logic |
| 0.7.0 | Configuration Layout Final | Mar  7 2026 | Per‑callout INI toggles (19) + TrafficStopAssist |
| 0.8.0 | Community Safety Expansion | Mar  7 2026 | Welfare Check & Stolen Police Vehicle added |
| 0.9.0 | Callout Interface Integration | Mar  7 2026 | Manual menu support via Callout Interface |
| 0.9.1 | Maintenance & Doc Cleanup | Mar  7 2026 | XML docs, exception logging, QA verified build |

---

## 🧩 Detailed Version Timeline

### **v0.1.0 – Project Foundation** ( July 30 2024 )
- Established core plugin structure with 18 original callouts.  
- Introduced logging, INI configuration, and basic LSPDFR API support.  

### **v0.1.5 – Radiant AI Behavior System** ( Sept 28 2024 )
- Implemented dynamic AI responses (Compliant / Flee / Hostile).  
- Added persuasion rolls and behavior balancing for SuicideAttempt.  

### **v0.2.0 – Narrative Enhancement Pack** ( Nov 5 2024 )
- Added dialogue trees and contextual events to callouts.  
- Expanded character models and introduced helpline overlay.  

### **v0.3.0 – Stability & Cleanup Framework** ( Jan 15 2025 )
- Added SafeCleanup method to every callout.  
- Fixed entity retention and improved forced cancel behavior.  

### **v0.4.0 – Logging & INI Upgrade** ( Mar 2 2025 )
- Introduced `LogLevel` configuration.  
- Added INI auto‑write for missing entries.  
- Began internal performance profiling.  

### **v0.5.0 – Integration Harmony Build** ( Jun 14 2025 )
- Integrated 9 external mods (STP, UB, CompuLite, Grammar Police, CalloutInterface, LSPDFR Expanded, Policing Redefined, Reports+, EPC).  
- Added conflict handler for Policing Redefined ↔ STP / UB.  
- Created `IntegrationDelegator.cs`.  

### **v0.6.0 – Variable Dispatch Cooldown** ( Oct 11 2025 )
- Introduced realistic callout cooldown range.  
- Logs selected delay after each scene.  
- Verified stable performance under load.  

### **v0.7.0 – Callout Toggle Return & Configuration Layout Final** ( Mar 7 2026 )
- Restored per‑callout INI toggles.  
- Moved SuicideAttempt to its own `[SuicideCallout]` section.  
- Added descriptive comments for helpline and EMS timers.  

### **v0.8.0 – Community Safety Expansion Build** ( Mar 7 2026 )
- Added new callouts: Welfare Check & Stolen Police Vehicle.  
- Expanded registrar and documentation.  
- Raised total callouts to 21.  

### **v0.9.0 – Callout Interface Menu Integration Build** ( Mar 7 2026 )
- Integrated Callout Interface API for manual callout selection.  
- Added `RegisterWithMenu()` and menu category label.  

### **v0.9.1 – Maintenance & Documentation Cleanup Build** ( Mar 7 2026 )
- XML docstrings added to all classes.  
- Replaced silent `catch {}` with logged warnings.  
- Added `safety blip delete` redundancy to cleanup methods.  
- Verified QA compliance ( memory leak‑free, 0 errors ).  
- Ready for public GitHub testing release package.  

---

## 🌳 Version Tree
