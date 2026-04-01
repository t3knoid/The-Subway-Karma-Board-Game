# The Subway Karma Game — Rules Reference

## Overview

A single-player (solitaire) board game set on the New York City subway system. The player moves a token clockwise around a 22-square perimeter loop by spinning a 6-segment wheel. Karma cards are collected at station squares. The game ends when the last karma card is drawn; the player's final karma score is the sum of all card point values.

---

## Components

| Component | Quantity | Notes |
| --- | --- | --- |
| Game board | 1 | 22-square perimeter loop, NYC subway map in center showing the Karma card draw pile |
| Karma cards | 26 | 10 positive, 10 negative, 6 special (no blank cards in deck) |
| Spinner | 1 | 6-segment wheel with fixed pointer |
| Player token | 1 | Starts on any Station square |

---

## Board

The board is a **closed perimeter loop of 22 squares played clockwise**. The center panel (titled "The Subway KARMA Game") is decorative and represents the karma card draw pile. The corner spaces represent subway stations. Each station is identified with the station location and line identifier. The rest of the square spaces represent the connections between the subway stations. Landing instructions on a square triggers an action the player must take.

### Square Types

| Type | Background | Font Color | Effect |
| --- | --- | --- | --- |
| Station | Black (subway-styled) shows the line identifier at the bottom of the square, the station location is just above the identifier with a white background | White for the line identifier and black for the station name | Draw a karma card when landed on or passed |
| Express Train | Black | White | Spin again; move that many additional spaces forward |
| Early Train | Green | White | Move 1 space ahead |
| Late Train | Green | Red | Lose a turn |
| Sick Passenger | Green | Red | Go back 1 space |

> **Font color encodes polarity:** White = positive/neutral effect. Red = negative effect.

### Complete Board Layout (clockwise, index 0–19)

| Index | Square Name | Type |
| --- | --- | --- |
| 0 | Broadway / 7 Avenue Local (1) 96th Street | Station ★ |
| 1 | Early Train - Move 1 Space Ahead | Early Train |
| 2 | Empty | NoOp |
| 3 | Late Train — Lose a Turn | Late Train |
| 4 | 6 Avenue Express (F) | Express Train |
| 5 | 6 Avenue Local / 47-50th Streets (F) | Station ★ |
| 6 | Sick Passenger — Go Back 1 Space | Sick Passenger |
| 7 | Empty | NoOp |
| 8 | Early Train — Move 1 Space Ahead | Early Train |
| 9 | Late Train — Lose a Turn | Late Train |
| 10 | 8 Avenue Express (A) | Express Train |
| 11 | Times Square / Eighth Avenue Local (C) | Station ★ |
| 12 | Early Train — Move 1 Space Ahead | Early Train |
| 13 | Empty | NoOp |
| 14 | Late Train — Lose a Turn | Late Train |
| 15 | Broadway Express (Q) | Express Train |
| 16 | Astoria Blvd / Broadway Local (N) | Station ★ |
| 17 | Sick Passenger — Go Back 1 Space | Sick Passenger |
| 18 | Empty | NoOp |
| 19 | Early Train — Move 1 Space Ahead | Early Train |
| 20 | Late Train — Lose a Turn | Late Train |
| 21 | Seventh Avenue Express (2) | Express Train |

★ = Station (corner square). Stations are at indices **0, 5, 11, 16**.

---

## Spinner

The physical spinner is a circle divided into **6 equal segments**, each labelled 1–6:

- **Red segments (1, 2, 3):** lower values
- **Green segments (4, 5, 6):** higher values

A **fixed arrow pointer** (pointing right, below the wheel) serves as the indicator. The wheel spins; the pointer does not move. Each segment has equal probability (1-in-6).

The spinner is used:

1. At the start of every turn to determine how many spaces to move.
2. As a **re-spin** when landing on an Express Train square — the result is added as extra spaces forward on top of the original move.

---

## Turn Sequence

1. **Spin** — tap/click the spinner. Result is 1–6.
2. **Move** — advance the token clockwise that many spaces.
3. **Collect karma cards** — draw one card for every Station square passed through or landed on during the move (in order).
4. **Apply square effect** — resolve the square the token landed on:
   - **Station:** card already drawn in step 3.
   - **Express Train:** spin again; move that many additional spaces forward (no further card draws for squares passed during the bonus move unless a Station is crossed).
   - **Early Train:** move 1 space ahead immediately.
   - **Late Train:** lose next turn.
   - **Sick Passenger:** move 1 space back immediately.
   - **Blank** considered a NoOp.
   - The resulting square the player lands on when **moving back** is considered a NoOp.
5. **End turn** — pass to the next player (or in solitaire, begin the next turn).

---

## Karma Cards

The deck contains **26 cards** (no blank cards). Blank cards in the physical set are print templates only and are excluded from play.

The deck is **shuffled using Fisher-Yates** at the start of every game and again on every "Play Again" reset.

### Positive Cards (+points)

| Card Title | Points |
| --- | --- |
| Finds Metrocard | +5 |
| Gets a Seat | +1 |
| Holds Door for Someone | +2 |
| Gives Seat to Elderly | +9 |
| Gives Seat to Pregnant Woman | +9 |
| Seats Next to Celebrity | +4 |
| Finds Money | +5 |
| Gives Money to Musician | +2 |
| Local Train Becomes Express | +1 |
| Finished Crossword | +1 |

### Negative Cards (−points)

| Card Title | Points |
| --- | --- |
| Loses Metrocard | −5 |
| Overcrowded Train | −1 |
| Fell Asleep, Got Robbed, and Woke Up Naked | −9 |
| Does Not Give Up Seat to Elderly | −9 |
| Rats! | −1 |
| Loses Wallet | −5 |
| Smelly Subway Car | −6 |
| Coffee Spills On You | −7 |
| Miss Your Stop | −1 |
| Does Not Give Up Seat to Pregnant Woman | −9 |

### Special Cards (0 points)

| Card Title | Count | Solitaire Behavior |
| --- | ---| --- |
| Instant Karma — Take a random card from every other player | 3 | No-op (no other players); display card, end turn |
| Says Hello to Other Riders — Sorry, no points for being human | 3 | No-op (0 pts); display card, end turn |

### Deck Composition Summary

| Category | Count | Point Range |
| --- | --- | --- |
| Positive | 10 | +1 to +9 |
| Negative | 10 | −1 to −9 |
| Special | 6 | 0 |
| **Total** | **26** | |

---

## Winning (Solitaire)

The game ends when **all 26 karma cards have been drawn**. The player's final score is the **sum of all collected karma card point values**.

There is no pass/fail threshold in solitaire — the score is the result. In a multiplayer game, the player with the **highest karma total wins**.

---

## Quick Reference

| Rule | Value |
| --- | --- |
| Total squares | 22 |
| Direction of play | Clockwise |
| Starting position | Any Station square (youngest player goes first in multiplayer) |
| Spinner range | 1–6 (equal probability) |
| Total karma cards | 26 |
| Cards drawn per Station | 1 (for each station passed or landed on in a single move) |
| Game end condition | Last karma card drawn |
