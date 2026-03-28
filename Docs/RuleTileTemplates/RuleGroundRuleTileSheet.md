# RuleGround RuleTile Sheet

- Tile size: 16x16
- Layout: 4x4, row-major by mask value
- Source sprites: `Assets/_Project/Art/Prototype/Ground/RuleGround/rule_tile_template_16px.psd` visible sprites named `rule_tile_template_16px_*`

| Row | Col | Mask | Source |
| --- | --- | --- | --- |
| 0 | 0 | `0000` | `rule_tile_template_16px_*` |
| 0 | 1 | `0001` | `rule_tile_template_16px_*` |
| 0 | 2 | `0010` | `rule_tile_template_16px_*` |
| 0 | 3 | `0011` | `rule_tile_template_16px_*` |
| 1 | 0 | `0100` | `rule_tile_template_16px_*` |
| 1 | 1 | `0101` | `rule_tile_template_16px_*` |
| 1 | 2 | `0110` | `rule_tile_template_16px_*` |
| 1 | 3 | `0111` | `rule_tile_template_16px_*` |
| 2 | 0 | `1000` | `rule_tile_template_16px_*` |
| 2 | 1 | `1001` | `rule_tile_template_16px_*` |
| 2 | 2 | `1010` | `rule_tile_template_16px_*` |
| 2 | 3 | `1011` | `rule_tile_template_16px_*` |
| 3 | 0 | `1100` | `rule_tile_template_16px_*` |
| 3 | 1 | `1101` | `rule_tile_template_16px_*` |
| 3 | 2 | `1110` | `rule_tile_template_16px_*` |
| 3 | 3 | `1111` | `rule_tile_template_16px_*` |

Mask meaning uses the current RuleTile setup:
- bit 1 = north
- bit 2 = east
- bit 4 = south
- bit 8 = west
