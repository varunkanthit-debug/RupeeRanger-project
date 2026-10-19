# RupeeRanger — Project Handover & Architecture Summary

> **Current Status**: All 11 scenes built, compiled with **0 errors**, and configured in Unity 6 Build Settings. All 6 mini-games and 16 town locations are fully implemented and integrated with the 30-day task loop.

---

## 1. Project Overview & Tech Stack

* **Game Title**: RupeeRanger
* **Engine / Version**: **Unity 6 (6000.5.1f1)**
* **Render Pipeline**: **Universal Render Pipeline (URP 2D)**
* **Genre**: 2D Financial Literacy Simulation & Educational Adventure Game for Indian Youth
* **Target Platforms**: PC / WebGL / Android (supports both Keyboard/WASD and Mobile Touch / Virtual Joystick)
* **Workspace Path**: `C:\Users\sit24\OneDrive\Documents\Unity projects\RupeeRanger1`

---

## 2. Scene Architecture (11 Scenes in Build Settings)

All scenes are configured in Unity's Build Settings (`EditorBuildSettings`) and can be auto-generated anytime via **Tools > RupeeRanger > Build Scenes**:

| Build Index | Scene Name | Description | Key Scripts |
|---|---|---|---|
| **0** | `Splash` | Intro splash, age bracket selection (5-7, 8-10, 11-14), character selection with cartoon portraits | `SplashScreenUI.cs`, `GameManager.cs` |
| **1** | `PlayerSetup` | Player profile name, avatar preview, and lifestyle setup | `PlayerSetupUI.cs`, `LifestyleConfig.cs` |
| **2** | `ExpenseSetup` | Budget allocation (Needs vs Wants vs Savings), pocket money / salary setup | `ExpenseSetupUI.cs` |
| **3** | `Town` | 2D Open-World Town with 16 locations, dynamic minimap, compass arrow, HUD, daily tasks | `TownHUD.cs`, `DayManager.cs`, `PlayerController2D.cs`, `TownBuilding.cs` |
| **4** | `Result` | End-of-month (Day 31) scorecard, financial health rating, savings summary, advice | `ScenePlaceholderUI.cs` |
| **5** | `MiniGame_WordSearch` | Additive mini-game: Drag & find hidden words across an 8x8 to 12x12 grid | `WordPuzzleMiniGame.cs` |
| **6** | `MiniGame_WordFinder` | Additive mini-game: Anagram puzzle forming words from given letter tiles | `WordFinderMiniGame.cs` |
| **7** | `MiniGame_MathSpeed` | Additive mini-game: Fast-paced arithmetic equations with keypad, score, and timer | `MathsGameMiniGame.cs` |
| **8** | `MiniGame_MathPuzzle` | Additive mini-game: 3x3 grid arithmetic puzzle with interactive cell solving | `MathsPuzzleMiniGame.cs` |
| **9** | `MiniGame_MemoryMatch` | Additive mini-game: Card-matching financial icons (Coin, Vault, Piggy Bank, Shield, etc.) | `MemoryMatchMiniGame.cs` |
| **10** | `MiniGame_QuizMaster` | Additive mini-game: 4-choice financial literacy quiz with instant feedback | `QuizMasterMiniGame.cs` |

---

## 3. Playable Mini-Games (6 Total)

Every game implements the base class `MiniGame.cs` (`Setup(DifficultyBand)`, `StartGame()`, `Complete(success)`, `event OnFinished`) and loads additively on top of the Town scene without losing the player's position or town state:

1. **Maths Speed Game** (`MathsGameMiniGame.cs` — Maths Speed House)
   * On-screen calculator keypad (no double-click bugs).
   * Operands scale by difficulty: Easy ($2\text{--}25$), Medium ($12\text{--}85$), Hard ($15\text{--}120$ with mixed $+$, $-$, $\times$, $\div$).
   * Non-repeating problem queue (`Queue<string> recentProblems`) remembers past 20 equations so questions never repeat.
2. **Maths Puzzle Grid** (`MathsPuzzleMiniGame.cs` — Maths Puzzle House)
   * 3x3 interactive arithmetic puzzle grid.
   * Every cell in the grid is guaranteed to have a unique equation (`usedInThisGrid` signature checking) seeded with random hashes.
   * Keypad solver with visual checkmarks and audio feedback on cell completion.
3. **Word Search (Word Puzzle)** (`WordPuzzleMiniGame.cs` — Word Search House)
   * Letter grid generated dynamically based on difficulty (8x8 up to 12x12).
   * 20+ curated themes (Rupee Savings, Banking, Smart Shopping, Technology, Wildlife, Careers).
   * Category tracking (`HashSet<string> playedWordSearchCategories`) prevents the same puzzle theme from repeating.
4. **Word Finder (Anagrams)** (`WordFinderMiniGame.cs` — Word Finder House)
   * Scrambled letter tiles that players tap or type to form valid English words into answer boxes.
   * 26+ curated anagram sets in `WordLists.json` across Easy (3 letters), Medium (4 letters), and Hard (5 letters).
   * Puzzle history tracking (`HashSet<string> playedPuzzles`) ensures a brand-new anagram every visit.
5. **Memory Match** (`MemoryMatchMiniGame.cs` — Arcade / Game Centre)
   * Card-matching memory game with 12 to 16 cards featuring financial concepts: Rupee Coin, Piggy Bank, Bank Vault, Insurance Shield, Growth Chart, Budget Wallet, Study Book, Trophy.
   * Move counter, game timer, card flip animations, and victory panel.
6. **Quiz Master** (`QuizMasterMiniGame.cs` — Town Library)
   * Designed specifically for kids: 2 easy questions per round on piggy banks, saving rupees, needs vs wants, bank safety, and smart shopping lists.
   * Target correct = 2: Rapid, encouraging, 2-question quiz session with instant feedback explanations and +500 Savings reward.

---

## 4. Town World & Locations (16 Locations Total)

The town map features paved roads, stone plazas, decorative trees, and 16 distinct locations with floating billboard badges, proximity detection, and interactive popup modals:

### Mini-Game Houses (Complete Daily Task on Game Finish)
1. **Maths Speed House** (North-West) &rarr; `MiniGame_MathSpeed`
2. **Maths Puzzle House** (West) &rarr; `MiniGame_MathPuzzle`
3. **Word Search House** (North) &rarr; `MiniGame_WordSearch`
4. **Word Finder House** (North-Center) &rarr; `MiniGame_WordFinder`
5. **Arcade / Game Centre** (South-West) &rarr; `MiniGame_MemoryMatch`
6. **Town Library** (North-East) &rarr; `MiniGame_QuizMaster`

### Town Exploration Destinations (Complete Daily Task on Visit)
7. **Player Home** (Center-West spawn location)
8. **Commerce College** (North Plaza)
9. **State Bank of Rupee** (Central Commercial Avenue)
10. **Supermarket / Shop** (Center Plaza)
11. **City Hospital** (East Medical Plaza)
12. **Friend's House** (South-East Residential Area)
13. **Central Bus Stop** (Central Transit Hub)
14. **Community Hall** (Civic & Cultural Center)
15. **Town Bakery** (Shopping & Nutrition)
16. **Central Park** (Leisure & Recreation Zone)

---

## 5. Navigation & Guidance System

1. **Orbiting Guide Arrow**:
   * Revolves around the player at a 1.6-unit radius, pointing directly toward the active building.
   * High sorting order (50) and glowing golden color with pulsating scale ensure it is never obscured.
2. **Building Destination Beacon**:
   * A bouncing cyan/golden downward arrow floating directly above the destination building entrance.
3. **Highlighted Road Path**:
   * Bold line renderer (0.28 width, sorting order 15) routing along paved highways.
4. **Real-Time Compass HUD**:
   * Live cardinal direction and distance in meters displayed in the HUD banner (e.g. `[North-East • 18m]`, switching to `[🎯 HERE! Press E]` when arrived).

---

## 6. Random Events System (`EventData.cs`, `RandomEventManager.cs`, `Events.json`)

* **EventData Architecture**:
  * Fields: `id`, `title`, `description`, `amount` (negative for expenses, positive for windfalls), `category` (`Emergency` vs `Windfall`), `lifestyleMultiplier` (`1.0x` Basic, `1.25x` Middle, `1.8x` High).
  * **16 Curated Events**:
    1. `event_viral_fever`: Viral Fever & Clinic Visit (-₹1,200 emergency, matches Screenshot 1)
    2. `event_phone_screen`: Cracked Phone Screen (-₹2,400 repair dilemma or pay now)
    3. `event_lab_fee`: Mandatory Department Lab Manual & Kit Fee (-₹850, matches Screenshot 2 deficit trigger)
    4. `event_hostel_repair`: Hostel TV Short-Circuit Repair (-₹650)
    5. `event_college_fee`: College Examination & ID Registration Fee (-₹1,500)
    6. `event_book_fee`: Reference Library & Book Bank Fee (-₹700)
    7. `event_watch_broken`: Wristwatch Glass & Battery Repair (-₹450)
    8. `event_friend_visit`: Childhood Friend Visit & Canteen Lunch (-₹500 / -₹150 dilemma)
    9. `event_party`: Batchmate Birthday Celebration Party (-₹800 / -₹100 dilemma)
    10. `event_textbook`: Second-Hand Textbook & Study Guide Bundle (-₹600 / -₹250 dilemma)
    11. `event_freelance_gig`: Freelance Graphic Design Poster Gig (+₹2,500 windfall)
    12. `event_tutoring_bonus`: Extra Tutoring Session Bonus (+₹800 windfall)
    13. `event_scrap_recycling`: Old Cartons & Newspaper Recycling Sale (+₹350 windfall)
    14. `event_honesty_reward`: Returned Lost Wallet Reward (+₹600 windfall)
    15. `event_grandpa_gift`: Grandparent Festival Pocket Money Gift (+₹1,000 windfall)
    16. `event_bicycle_puncture`: Bicycle Puncture & Tube Replacement (-₹200)
* **Popup Presentation**:
  * Matches Screenshot 1 with blue header banner, category badge (`⚠ SUDDEN EMERGENCY` or `🎁 SURPRISE WINDFALL`), title, full story description, and single mandatory button (`PAY MANDATORY EXPENSE (-₹ 1,200)` / `CLAIM WINDFALL (+₹ 2,500)`) or 2-choice decision dilemmas.
  * **Event Rules**:
    * Each event appears **at most once per game** (`HashSet<string> seenEventIds`).
    * **No daily tasks on event days**: Player resolves the event and the day's progression is fulfilled.
    * Added to the HUD **Transaction Feed** with colored status text.

---

## 7. Loan System (`LoanSystem` & `TownHUD.cs`)

* **Deficit Alert Modal (Matches Screenshot 2)**:
  * Triggers automatically when player savings drop below ₹0 after any transaction or event payment.
  * Displays:
    * **Deficit to Cover**: `-₹ 90` (or actual negative amount)
    * **Approved Cash Relief**: `+₹ 2,000`
    * **Processing Surcharge**: `₹ 100`
    * **Monthly Interest Rate**: `5% Compounding`
    * **Total Repayment Debt**: `₹ 2,200`
    * **Credit Rating Impact**: `-60 Points`
    * Action Button: **ACCEPT EMERGENCY STUDENT LOAN**
  * **On Accept**:
    * Adds `+₹ 2,000` relief to savings (net ₹ 1,910, matches Screenshot 3).
    * Sets debt to `₹ 2,200` (red text in HUD).
    * Deducts `60 points` from Credit Rating / Score.
    * Displays floating animated `-60` feedback.
    * Logs loan credit and fee to the transaction feed.
* **Compounding Interest**:
  * 5% monthly compounding interest compounds as days pass (`DayManager.AdvanceNextDay()`), calculating daily interest and logging penalties.
* **Bank Loan Repayment**:
  * Visiting the **State Bank of Rupee** (`[E] Enter`) with active debt opens the **Loan Repayment Desk**.
  * Options: **Pay in Full** (+50 Score bonus) or **Pay Installment** (+25 Score bonus) when player savings permit.

---

## 8. Score System & Floating HUD Feedback

* **Score Logic**:
  * Task Completion: `+50 Points`
  * Wise Event Decisions: `+20` to `+45 Points`
  * Early Loan Repayment: `+50 Points` (full) / `+25 Points` (installment)
  * Emergency Loan Deficit: `-60 Points`
  * Unpaid Overdue Debt: `-2 Points/day` interest penalty
* **Floating Animated HUD Feedback**:
  * Animated floating feedback on the Town HUD with smooth vertical upward drift and gradual alpha fade over 1.35 seconds.
  * Distinct vibrant styling: Emerald Green with `+` prefix for positive scores, Crimson Red with `-` prefix for score penalties.

---

## 9. Daily Loop & Progression (30 Days)

* **Task & Event Management** (`DayManager.cs` & `DayPlans.json`):
  * Dynamic schedule containing standard task days (rotating MiniGames and Town Locations) and dedicated Event Days (Days 4, 5, 12, 16, 19, 21, 22, 26 matching project screenshots).
  * Day 12 showcases the mandatory Viral Fever Clinic Visit (-₹1,200).
  * Day 26 triggers the Lab Manual Fee leading to the Deficit Alert loan test.
  * Pressing **Next Day** advances the calendar, applies daily compounding interest on any active debt, updates financial meters, and deducts living costs.
  * Day 31 automatically loads the **Result** scene.

---

## 10. Result Scene & AdviceEngine (`ResultUI.cs` & `AdviceEngine.cs`)

* **Visual Architecture (Matches Phase 7 UI Design)**:
  * **Top Bar**: Logo `<color=#FF4081>₹</color> RupeeRanger` and interactive dark/light theme toggle button.
  * **Hero Header**: Gold trophy/star icon, bold `Game Over!` title, and personalized congratulatory subtitle: `Congratulations <name>! You completed 30 days of financial survival.`
  * **Final Score Card**:
    * Count-up animation easing from 0 to final score over 1.6s.
    * Formula display: `Score = (Savings × 2) - (Loan × 1.5)`.
  * **Character Stage & Emotions**:
    * Displays selected character portrait (Penny, Eco, Tech, Royal).
    * Three distinct emotional states evaluated against lifestyle score thresholds (`Student`, `Frugal`, `Middle`, `Luxury`):
      * **HAPPY**: Cheerful vertical squash/bounce animation, gold halo aura, cascading colorful confetti particles, and high score celebration popup modal.
      * **SAD**: Gloom blue tint, drooping sad sigh bob, budget deficit warning.
      * **NEUTRAL**: Calm idle breathing animation.
  * **AI Financial Advice (Prominently at the Top)**:
    * Powered by `AdviceEngine.cs` featuring **17 child-friendly financial rules** analyzing:
      * Entertainment spending > 15%
      * Low ending savings < 10%
      * Critical negative deficit
      * Emergency loan taken & interest accrued
      * Living/Rent costs > 38%
      * Dining out / cafeteria costs > 28%
      * Transit costs > 15%
      * Impulse spending on optional dilemma events
      * Prudent event trade-offs & side-hustle windfalls
      * Golden 70/20/10 budget balance
      * Champion super saver (> 30% savings with zero loans)
    * Dynamically prioritizes and renders the **top 3 most relevant tips as large, clear cards** with category badges, icons, and actionable advice.
  * **Financial Summary & Player Info Cards**:
    * Side-by-side comparative cards:
      * **Financial Summary**: Total Income (`₹{monthlyIncome:N0}`), Total Expenses (`-₹{totalAllExpenses:N0}` including budget setup, event costs, and loan interest), Final Savings (`₹{savings:N0}`), and Total Loan Taken (`₹{totalLoanTaken:N0}`).
      * **Player Info**: Player Name, Lifestyle, Monthly Income, Days Played (`30 days`), and Random Events count.
  * **Events Encountered List (No Duplicates)**:
    * 2-column grid rendering every unique event encountered throughout the 30-day adventure with green `+₹...` for windfalls and pink/red `-₹...` for expenses.
  * **Lifetime Career Progression & Character Unlocks**:
    * Accumulates final score into career `lifetimeScore`.
    * Compares against unlock requirements (`Eco: 300 pts`, `Tech: 600 pts`, `Royal: 1000 pts`).
    * Displays **"🎉 New Character Unlocked!" popup modal** when thresholds are reached, featuring portrait, name, and description.
  * **Navigation Buttons**:
    * `Play Again` (restarts fresh game session).
    * `Main Menu` (returns to Splash scene).
  * **Direct-Play Resilient Fallback**:
    * Self-constructing UI hierarchy (`BuildRuntimeUIHierarchy()` & `AutoBindElementsIfUnassigned()`) automatically populates demo data and constructs all UI elements if `Result` scene is launched directly in the Editor.
    * In Town scene, debug key **`[R]`** instantly transitions to the Result scene for immediate end-to-end testing.

---

## 11. Key Data Files & Assets

* **Word Database**:
  * `Assets/Resources/WordLists.json` & `Assets/Data/WordLists.json`: Contains all 20+ Word Search puzzles and 26+ Word Finder anagram sets.
* **30-Day Schedule**:
  * `Assets/Data/DayPlans.json`: 30 unique day entries rotating all 6 games and 10 destinations.
* **ScriptableObjects**:
  * `Assets/Data/Tasks/Task_Day1.asset` ... `Task_Day30.asset`: Runtime task definitions.
  * `Assets/Data/Lifestyles/`: Student, Frugal, Balanced, Luxury configurations.
  * `Assets/Data/Characters/`: Penny, Eco, Tech, Royal cartoon characters with high-resolution portraits.
* **Sprites**:
  * `Assets/Sprites/Town/`: Pixel-art building sprites for all 16 locations, roads, paths, trees, joystick, and interact buttons.
* **Scene Builder**:
  * `Assets/Scripts/Editor/RupeeRangerBuilder.cs`: Single-file editor tool with `BuildAllScenesFromMenu`, `BuildResultScene`, and `BuildAllScenesBatch`.

---

## 12. How to Resume Work in a New Chat

To start the next chat seamlessly, paste this quick prompt:

```text
We are continuing work on the Unity 2D game "RupeeRanger1" (Unity 6000.5.1f1 URP).
All 11 scenes (Splash, PlayerSetup, ExpenseSetup, Town, Result, and 6 MiniGames: Word Search, Word Finder, Maths Speed, Maths Puzzle, Memory Match, Quiz Master) are built and functional with 0 compile errors.
Please refer to the architecture in `project_summary.md` and await my instructions.
```
