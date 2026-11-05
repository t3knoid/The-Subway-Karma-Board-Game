# The Subway Karma Board Game
 A board game that last as long as a NYC subway ride.

![The Subway Karma Board Game](subway_karma.png)

## Introduction

Riding the subway seems like an adventure every time I ride it. The Subway Karma board game hopes to recreate this feeling of adventure anytime. I designed this game during a game design course. I wanted to create a game that I can closely relate to at that time. I also wanted it to be simple and allow the player to customize the length of play by adding or removing karma cards.

## Board Setup

Print `Subway Karma Game.pdf` at 100% (actual size), preferably in color and on card stock. It fits standard 8.5" x 11" paper: page 1 is the board, page 2 is a cut-out spinner (you'll need a brass paper fastener). The karma cards in `Karma Cards.pdf` use a standard business card layout (Avery 5371). Avery 5371 business card stock gives a more professional look.

The board, cards and README image are all generated from the digital game's artwork and data (`tools/make_board.py`, `tools/make_cards.py`, `tools/make_printables.py`).

I never got a chance to design any game pieces so you'll have to be creative here. The piece would have to be small enough to fit on the board as to not crowd it especially when there are more than one piece on a square.

## Playing

1. Stack the karma cards in the middle of the game board face down.
2. Start on any station square. Youngest player goes first. Direction of play is clockwise.
3. Use spinner to dictate number of squares to move.
4. Follow instructions on the square you land on
5. Pick up a karma card whenever you land or pass a station. Keep the karma card.
6. Game ends after all karma cards are used.
7. Sum up karma card points at the end of the game. 
8. The player with the most points wins.

## Customizing

Print `Karma Cards.pdf` at 100% scale on Avery 5371 business-card stock (or plain card stock, then cut along the guides).

To add or change cards, edit the deck in `SubwayKarmaGame/Assets/Scripts/Rules/StandardGame.cs` (the digital game uses the same list), then regenerate the printable sheets:

```sh
python -I tools/make_cards.py
```

## Digital Version

A Unity version of the game lives in [`SubwayKarmaGame/`](SubwayKarmaGame/).

- [Development Guide](docs/DEVELOPMENT.md): requirements, building, running, testing and debugging
- [Building](docs/BUILDING.md): platform requirements and how to build for Android, iOS, Web, Windows, macOS and Linux
- [Game Rules](docs/GAME_RULES.md): the rules spec the digital version implements
