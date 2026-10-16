"""Apply the author's dirtboard hardware correction using the preserving batch path."""
import argparse

import move_reviewed_tools as batch
import organize_camp_items as guard

IDS={
    'proposal.wind.sail_panel','proposal.wind.folding_frame','proposal.wind.sail_grip',
    'proposal.wind.wheel','proposal.legger.board_bracket','proposal.t4.spar_joint',
    'proposal.t4.wheel_hub','proposal.collection.dirtboard_running_gear_kit',
    'proposal.collection.dirtboard_lane_snag_bar',
}
REASON='Author correction October 15: dirtboard parts and sail components in Tools move to Salvage, including supporting bracket, running-gear kit and named lane snag bar. The complete dirtboard and handheld dirtboard sail remain Tools. Category does not change IDs, tiers, recipe roles or gameplay behavior.'


def configure():
    batch.OUT=batch.migration.ROOT/'Logs/DirtboardPartsSalvage20261015'
    batch.AUDIT=batch.migration.ROOT/'Docs/Design/Items/DIRTBOARD_PARTS_SALVAGE_CLASSIFICATION.json'
    batch.SELECTED=IDS
    batch.REASON=REASON


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('action',choices=['prepare','apply','run'])
    action=parser.parse_args().action
    configure()
    if action=='run':
        guard.OUT=batch.OUT;guard.prepare=batch.prepare;guard.apply=batch.apply;guard.run()
    else:batch.prepare() if action=='prepare' else batch.apply()
    if action!='prepare':batch.update_manifest()
