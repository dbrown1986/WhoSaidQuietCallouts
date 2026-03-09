# 🧾 Who Said Quiet Callouts – CHANGELOG  
**Project Lifetime:** Initial Release → March 7 2026  
*(Maintained for LSPDFR / RAGE Plugin Hook Builds)*  

---

## 🧱 v0.1.0 Alpha – Project Foundation (Initial Release)
**Status:** Released · Framework Built · Core Architecture Complete

### Highlights
- Established base WSQ plugin structure: `Main.cs`, `Logger.cs`, Manifest, and 18 initial callouts.  
- Implemented standard LSPDFR `Callout` class inheritance.  
- Added INI configuration support (`WhoSaidQuietCallouts.ini`).  
- Introduced logging system (`WSQLogger`) and initial integrations (STP, CompuLite, Grammar Police, Callout Interface).  
- 4 random spawn points per callout.  
- Operational callouts: Armed Robbery, Domestic Disturbance, Kidnapping, Burglary, Animal Attack, etc.  

---

## 🧩 v0.1.5 Alpha – Radiant AI Behavior System
**Status:** Internal Update · Enhanced Immersion

### Additions
- Introduced `BehaviorState` enum (Compliant / Flee / Hostile).  
- Enabled probabilistic AI responses per callout.  
- Adaptive suspect decision logic.  
- Expanded Suicide Attempt callout prototype with persuasion system.  

---

## 🧠 v0.2.0 Alpha – Narrative Enhancement & Character Variety
**Status:** Major Expansion

### New Features
- Added full narrative context per callout with dialogue and backgrounds.  
- Introduced 24 ped models for Suicide Attempt variety.  
- Added helpline overlay for emergency awareness.  
- Weighted AI behavior probabilities for realism.  

---

## ⚙️ v0.3.0 Alpha – Stability & Safe Cleanup Framework
**Status:** Production Patch

### Improvements
- Implemented `SafeCleanup()` signature for harsh abort events.  
- Hooked `OnDutyStateChanged` for auto cleanup.  
- Removed ghost entities and alternate threads.  
- Stopped minor LSPDFR hangs on forced scene end.  

---

## 🔧 v0.4.0 Alpha – Advanced Logging and INI Upgrades
**Status:** Feature Upgrade

### Features
- Added `LogLevel (0–3)` hierarchy for verbosity.  
- Automatic INI creation/self‑repair on missing keys.  
- Added console warnings and self‑healing error routines.  
- Internal performance optimizations.  

---

## 🔌 v0.5.0 Alpha – Integration Harmony Build
**Status:** Major Refactor · Cross‑Mod Compatibility Version

### Highlights
- Added 9 plugin integrations:  
  STP, UB, CompuLite, Grammar Police, Callout Interface, LSPDFR Expanded, Policing Redefined, Reports+, External Police Computer.  
- Introduced `IntegrationDelegator.cs` for centralized logic routing.  
- Conflict Detection: Policing Redefined vs Stop The Ped / Ultimate Backup.  
- In‑game notifications and log tracking for integration status.  
- Enforced Single Active Callout System for stability.  

---

## ⏱ v0.6.0 Alpha – Variable Dispatch Cooldown Build
**Status:** Stable

### Additions
- Replaced `CalloutCooldownSeconds` with:  
  `MinCalloutCooldownSeconds=30` and `MaxCalloutCooldownSeconds=300`.  
- Random dispatch interval per cycle for realism.  
- Added INI auto‑writes for these new values.  
- Log message records selected cooldown.  

---

## 🧩 v0.7.0 Alpha – Callout Toggle Return & Configuration Layout Final
**Status:** Configuration Overhaul

### Changes
- Re‑introduced per‑callout INI toggles (19 entries).  
- Moved Suicide Attempt to separate `[SuicideCallout]` section for clarity.  
- Cleaned spacing and comment format.  
- Added callout INI entries for TrafficStopAssist.  
- Enhanced comment details for `EnableHiddenEMSTimer` and `EnableHelplineOverlay`.  

---

## 🌆 v0.8.0 Alpha – Community Safety Expansion Build
**Status:** Expansion Release

### Additions
- Added callouts:  
  - **Welfare Check** – Code 2 residential assist scenario.  
  - **Stolen Police Vehicle** – Code 3 pursuit response.  
- Updated INI [Callouts] section to include both entries.  
- Updated Registrar and auto‑default arrays.  
- Total callouts: 21.  

---

## 🧭 v0.9.0 Alpha – Callout Interface Menu Integration Build
**Status:** Feature Upgrade

### Features
- All 21 callouts now listed in manual Callout Interface menu.  
- Added `RegisterWithMenu()` method in `CalloutInterfaceIntegration.cs`.  
- Updated `CalloutRegistrar` to publish callouts after registration.  
- Maintains full auto‑dispatch and integration compatibility.  

---

## 🧱 v0.9.1 Alpha – Maintenance & Documentation Cleanup Build (03/07/2026)
**Status:** Current Master · Stable Release  

### Fixes & Improvements
- Added doc‑style XML summaries to classes for IntelliSense.  
- Replaced silent `catch {}` blocks with WSQLogger warnings.  
- Added redundant `try { sceneBlip.Delete(); }` safety calls to cleanup methods (no orphan blips).  
- Normalized spacing and comments in Registrar & Logger.  
- Verified no memory leaks or overflow conditions.  
- QA tests passed – Ready for GitHub public release.  

---

## 🌳 Version Tree (Condensed)
