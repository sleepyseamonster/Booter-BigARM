# RuleGround RuleTile Sheet

- Tile size: 16x16
- Layout: 4x4, row-major by mask value
- Source sprites: `Assets/_Project/Art/Prototype/Ground/RuleGround/RuleGround00.png` through `RuleGround15.png`

| Row | Col | Mask | Source |
| --- | --- | --- | --- |
| 0 | 0 | `0000` | `RuleGround00.png` |
| 0 | 1 | `0001` | `RuleGround01.png` |
| 0 | 2 | `0010` | `RuleGround02.png` |
| 0 | 3 | `0011` | `RuleGround03.png` |
| 1 | 0 | `0100` | `RuleGround04.png` |
| 1 | 1 | `0101` | `RuleGround05.png` |
| 1 | 2 | `0110` | `RuleGround06.png` |
| 1 | 3 | `0111` | `RuleGround07.png` |
| 2 | 0 | `1000` | `RuleGround08.png` |
| 2 | 1 | `1001` | `RuleGround09.png` |
| 2 | 2 | `1010` | `RuleGround10.png` |
| 2 | 3 | `1011` | `RuleGround11.png` |
| 3 | 0 | `1100` | `RuleGround12.png` |
| 3 | 1 | `1101` | `RuleGround13.png` |
| 3 | 2 | `1110` | `RuleGround14.png` |
| 3 | 3 | `1111` | `RuleGround15.png` |

Mask meaning uses the current RuleTile setup:
- bit 1 = north
- bit 2 = east
- bit 4 = south
- bit 8 = west
