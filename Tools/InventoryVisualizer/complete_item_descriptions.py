"""Expand terse catalog descriptions without changing item mechanics or identity."""
import json
import shutil
import sys

import organize_camp_items as guard
import split_equipment as migration
from workbook_edits import atomic_write, digest, parse_sheets

OUT=migration.ROOT/'Logs/ItemDescriptions20261016'
AUDIT=migration.ROOT/'Docs/Design/Items/ITEM_DESCRIPTION_COMPLETION_2026-10-16.json'
PREVIOUS_AUDIT=None

# Explicit prose for records whose old description was only a name or a vague role.
SPECIFIC={
 'lab.salvage.drive_assembly':'A drive assembly removed from a dead machine, containing the fitted parts that once transmitted motion through its mechanism. It is a source of possible repair parts or recoverable material; removal does not establish that the assembly still works.',
 'lab.salvage.metal_plate':'A metal plate recovered from the dead machine’s casing or structure. Its flat section is useful as a candidate fabrication blank, patch or source of metal after inspection; alloy, usable condition and fitting requirements remain open.',
 'lab.salvage.wire_bundle':'A bundle of conductors recovered from a dead machine. Individual lengths are candidates for electrical repairs and rewiring after their continuity, insulation and material have been checked.',
 'equipment.algae_canister':'Booter’s attached living-algae reserve, carried for eating, drinking and treating injuries. The canister holds the survival supply rather than creating a separate meal or drink each time it is used; its existing attachment and reserve rules still apply.',
 'equipment.cloak':'Booter’s worn cloak, which also serves as the blanket when he settles down to rest. It is the same owned item in both uses, so laying it out does not create a second piece of equipment.',
 'equipment.digging_tool':'A combined shovel and axe carried as Booter’s starter excavation tool. The broad working end and cutting edge suggest digging and rough cutting uses; the current inventory definition does not itself add mining requirements or a combat action.',
 'tool.micro_dust_harvester':'A portable dust canister that can be deployed to collect airborne dust for later processing. Its stored dust remains inside the same physical canister during deployment and recovery, making the filled vessel part of the expedition’s haul.',
 'equipment.grappling_hook':'A hooked terrain-navigation tool intended to establish a hold on suitable features during traversal. Its line, anchoring, range and movement behavior require design; owning the item does not currently enable a grapple traversal action.',
 'equipment.arc_battery_charged':'An Arc Battery containing refined Arc Energy. It represents the charged state of a reusable battery rather than a supply of loose energy; compatible consumers and their operating requirements remain to be defined.',
 'equipment.arc_battery_empty':'An Arc Battery with no usable charge, retained for charging in the dust refinery. The existing holder must be supplied to the process; charging changes its state rather than manufacturing an extra battery.',
 'tool.popup_camp':'A reusable packed shade arrangement that can be deployed as a resting place for Rest and Sleep. It remains one physical camp item through packing and deployment, with no additional healing or remote-storage function implied by its shelter role.',
 'resource.airborne_dust':'Dust suspended in the atmosphere and gathered with the dust canister for the camp refinery. It is the recorded energy-processing feed, distinct from the ordinary Mineral Dust produced by sorting Ironstone.',
 'lab.ironstone.aggregate':'The coarse granular fraction left by dry sorting Ironstone. Its larger pieces form a separate stock from the sand, mineral fines and iron-rich concentrate; downstream uses and recovery yields remain to be designed.',
 'lab.ironstone.concentrate':'The iron-rich fraction separated during dry sorting of Ironstone. It concentrates candidate metal-bearing feed for later refining, without implying that sorting has already produced usable iron metal.',
 'resource.ironstone_ore':'Bulk iron-bearing rock gathered from exposed Ironstone deposits. It feeds the dry-sorting process, which separates several material fractions; it is raw mineral cargo rather than ready-to-use metal.',
 'lab.ironstone.mineral_dust':'Fine mineral particles separated during dry sorting of Ironstone. This is ordinary mineral stock with an unresolved downstream use, not the airborne energy-bearing dust accepted by the Arc refinery.',
 'lab.ironstone.sand':'The sand-sized fraction separated during dry sorting of Ironstone. It is stored independently from coarse aggregate and mineral dust so later processing can use the appropriate grain size; a specific downstream recipe is not yet assigned.',
 'proposal.bio.fat':'A withheld hypothesis for a fat-like faunal tissue. The source anatomy has not been established, so this record is a question for biological design rather than an available cooking ingredient, lubricant or fuel.',
 'proposal.bio.wax':'A conditional solid biological material whose proposed fat-like precursor remains unverified. A source and preparation route must be established before any sealing, coating or other practical use can be assigned.',
 'proposal.audit.pressed_hide_fastening_strips':'Narrow strips of scarce processed hide intended for garment fastenings and flexible retaining ties. They join clothing pieces where a rigid metal fitting would be awkward; strip dimensions, attachment method and working strength remain open.',
 'proposal.audit.drawn_steel_music_wire_stock':'Drawn steel wire stock proposed for strings and other tensioned parts in musical instruments. It provides a candidate alternative to scarce biological strings; wire grade, working tension and acoustic tuning still require design.',
 'proposal.audit.graphite_electrode_plate_stock':'Graphite stock prepared as flat electrode blanks for electrical assemblies. The plates provide candidate conductive surfaces that can be fitted into a designed cell or contact arrangement; chemistry, connections and operating ratings remain unresolved.',
 'proposal.audit.shaped_metal_grip_blank':'A shaped metal blank awaiting its final grip profile and mounting holes. It supplies the handle body for a tool or other handheld assembly; the finished fit and contact surface depend on the equipment receiving it.',
 'proposal.audit.linked_steel_mail_rings':'Small steel rings linked into a flexible mail section for protective fabrication. The linked structure can follow body movement while covering an area; ring closure, backing, coverage and protective performance remain design questions.',
 'proposal.audit.small_metal_hinge_pair':'A matched pair of small metal hinge fittings for a folding or opening assembly. They establish a controlled pivot between two parts; pin size, attachment points and load capacity must be chosen for the host item.',
 'proposal.audit.bent_wire_spectacle_frame':'A bent-wire frame intended to hold a matched pair of spectacle lenses in front of the eyes. Bridge shape, lens retention and the wearer’s fit still need design; the frame alone supplies no optical correction.',
 'proposal.audit.fired_ceramic_vessel_blank':'A fired ceramic body awaiting the finishing and fittings needed for a particular vessel. It provides the shaped container shell; seals, contents compatibility and any service rating must be resolved for its intended use.',
 'proposal.audit.membrane_tension_hoop':'A metal hoop intended to retain a prepared membrane under tension on an instrument. It holds the playing surface around its edge; membrane selection, tightening hardware and tuning remain open.',
 'proposal.audit.cut_metal_reed_set':'A set of thin cut metal tongues proposed as vibrating reeds for a musical instrument. Airflow excites the fitted reeds to produce sound; dimensions, mounting and tuning have not yet been specified.',
 'proposal.audit.ceramic_whistle_mouthpiece':'A ceramic mouthpiece shaped to guide a player’s breath into a whistle or chamber instrument. Its air channel and edge help form the sound; final geometry and the matching resonant body remain to be designed.',
 'proposal.audit.threaded_drive_screw':'A threaded screw intended to turn rotary input into controlled linear movement inside a mechanism. It needs a matched threaded member, supports and a suitable drive; travel, fit and loading remain unresolved.',
 'proposal.audit.hand_drill_spindle':'The rotating spindle at the center of a hand-drill assembly. It carries motion to a fitted drilling tool; the bit connection, bearings and means of turning the spindle must be matched to the drill.',
 'proposal.audit.replaceable_drill_bit_insert':'A replaceable cutting insert intended for the working end of a drilling tool. It supplies the wear surface that contacts the workpiece; material, fastening and the compatible drill geometry remain to be selected.',
 'proposal.audit.compact_latch_and_trigger_pack':'A compact mechanical pack combining a retaining latch with a release trigger. It holds and releases a matched mechanism on command; travel, reset method and safe loading depend on the host equipment.',
 'proposal.audit.valve_poppet_blank':'An unfinished valve poppet intended to meet a matching seat inside a flow-control assembly. Its final sealing face and stem require fitting; working medium, pressure and compatibility have not been assigned.',
 'proposal.audit.fitted_pressure_barrel_module':'A barrel module proposed for a matched pressure-driven assembly, using ceramic and metal parts. It guides the designed moving medium or projectile; seals, pressure source and safe operating conditions remain unresolved.',
 'proposal.audit.ceramic_backed_control_switch':'A control switch with conductive contacts supported on a ceramic backing. It opens or closes a designed electrical path while keeping the mounting surface separate from the contacts; ratings and wiring remain to be established.',
 'proposal.audit.fitted_machine_casing':'A fitted metal casing intended to surround and locate the parts of a particular machine. It provides mounting and enclosure surfaces; openings, access, seals and internal clearances depend on the mechanism it houses.',
 'proposal.audit.supported_iron_heating_element':'An iron heating element carried on ceramic and metal supports. It is proposed for a controlled electrical-heating assembly; the power source, heat control and safe service conditions remain unresolved.',
 'proposal.audit.metalworking_hand_saw':'A hand-operated saw intended for cutting suitably matched metal stock. Its toothed blade and frame provide a candidate workshop cutting tool; blade manufacture, supported materials and useful cutting capacity require design.',
 'proposal.audit.workshop_winding_mandrel':'A workshop mandrel used as a forming support while winding wire into a chosen coil shape. It controls the form during preparation and is removed or retained according to the assembly; winding dimensions and conductor treatment remain open.',
 'proposal.container.reagent_bottle':'An opaque bottle recovered as ordinary salvage. Its covered body was intended to shield stored material from view or light, but any remaining contents and closure condition are unknown. Reuse or recovery of its material requires inspection; no special payload or sealing behavior is assigned.',
 'proposal.container.wide_jar':'A wide-mouth jar retained as ordinary salvage. The broad opening gives access to its interior for inspection and possible reuse, while chips, residue and the condition of its closure must be checked. No sample-handling capacity or contents compatibility is assigned.',
 'proposal.container.ampoule':'A small sealed ampoule retained as ordinary salvage. Its enclosed form suggests a former sample holder, but the contents and integrity are unknown. It may offer recoverable material after inspection, without providing a special sample-storage or opening mechanic.',
 'proposal.container.marked_flask':'A glass flask bearing visible measurement or identification marks, recovered as ordinary salvage. The markings remain part of the object’s physical history; they do not establish accurate calibration. Possible reuse and material recovery depend on its condition.',
 'proposal.container.shell_cage':'A glass flask surrounded by a shell cage, retained as ordinary salvage. The outer pieces suggest a guard around the fragile vessel. The cage, glass and fittings may have separate reuse or recovery value, but protection, sealing and payload capacity remain unassigned.',
 'proposal.container.lined_canister':'A haul canister with a glass-lined interior, recovered as ordinary salvage. Its lining separates the former contents from the outer body, though cracking, residue and closure condition are unknown. Any useful reuse or material recovery requires inspection.',
 'proposal.container.ceramic_jar':'A glazed ceramic jar retained as ordinary salvage. Its hard body and glazed inner surface suggest a former material-storage vessel. Cracks, residue and closure fit must be checked before reuse; no chemical compatibility or special container behavior is assigned.',
 'proposal.container.sealed_box':'A closed cargo box recovered as ordinary salvage. The enclosure and fastening hardware may be useful parts, but the former contents and the condition of its closure are unknown. It does not add a special storage capacity or guaranteed protective seal.',
 'proposal.container.draw_tube':'A narrow glass tube retained as ordinary salvage from a former material-transfer arrangement. Its hollow shape may offer a reusable small part or recoverable glass after inspection. No pumping, safe sampling or liquid-transfer behavior is assigned.',
 'proposal.container.glass_stopper':'A shaped glass stopper retained as ordinary salvage. Its ground or formed surface was intended to meet a matching vessel opening. A loose recovered stopper has no guaranteed mate or seal; fit and condition must be checked before reuse.',
 'proposal.collection.divided_ceramic_reagent_caddy':'A ceramic caddy with divided recesses, recovered as ordinary salvage. The divisions once separated small vessels or working materials. The body may be reused or recovered as ceramic feed after inspection, without assigning chemical compatibility, contents or special storage capacity.',
}

RAW={
 'pumice':'Porous mineral lumps proposed as abrasive feed for rubbing and surface preparation. Selection and grading determine which pieces could become useful finishing material.',
 'feldspar':'Fragments of feldspar-bearing mineral proposed for glass and ceramic batches. They must be selected and prepared as a controlled mineral feed rather than treated as a finished glass or ceramic material.',
 'spines':'Loose hard faunal spines proposed as small pointed blanks for tools and fittings. Their shape offers a starting point for fitting, while source anatomy and usable strength remain unverified.',
 'claws':'Hard claw tips proposed as cutting or scraping blanks. A usable tip would need selection and a secure mounting before it could become part of a tool.',
 'brood_casing':'Empty faunal brood casings proposed as lightweight hard-part feed. Their shells could be inspected for reusable sections or processed material; species, dimensions and strength remain unresolved.',
 'antenna_sheath':'Empty antenna sheaths proposed as small hollow hard-part blanks. Their form suggests sleeves or fitted covers, subject to source anatomy, condition and useful dimensions.',
 'wing_ribs':'Rigid insectoid wing supports proposed as slender fabrication blanks. Their existing shape may reduce shaping work for a matched part, but species, size and load-bearing suitability remain open.',
 'ceramic_shards':'Broken ceramic pieces collected as secondary mineral feed. Sorting separates useful fragments for possible ceramic processing or material recovery; a broken vessel is not restored by collecting its shards.',
 'glass_shards':'Broken glass pieces collected for selection and possible glassworking feed. Color, composition and contamination must be checked before pieces can be combined into a controlled remelt batch.',
 'spring_strips':'Strips of recovered spring metal proposed for small resilient fittings. Surviving spring behavior must be checked before reuse; damaged lengths may instead be recovered as metal feed.',
 'metal_cable':'Lengths of recovered metal cable proposed for tensioned connections and bindings. Fraying, corrosion, end fittings and load suitability determine whether a length can be reused.',
 'metal_mesh':'Open metal mesh recovered as scrap. It offers candidate screening, retaining or reinforcement material where its aperture, alloy and remaining condition suit the intended assembly.',
 'spent_refractory':'Chunks of used heat-resistant lining recovered from prior hot-work equipment. They are candidate mineral feed for further preparation; contamination and heat damage prevent assuming the original refractory performance.',
 'obsidian':'Chunks of volcanic glass proposed as hard cutting and scraping blanks. Selection and controlled shaping could produce a useful edge, while brittleness and a suitable mounting remain constraints.',
 'emery':'Mineral lumps containing candidate abrasive grains. Crushing and grading would prepare a finishing feed; usable hardness, contamination and grain distribution require qualification.',
 'barite':'Dense mineral lumps proposed as a specialist fabrication feed. Their weight and mineral content offer a material-design starting point, with processing and the actual application still unresolved.',
 'grinding_pebbles':'Rounded stones selected as candidate crushing or grinding media. Their hard surfaces could reduce other materials mechanically, provided wear, contamination and the matching vessel are addressed.',
 'horn_core':'Hard faunal horn cores proposed as shaped blanks for tools or fittings. The retained form may support a useful working piece, but anatomy, preparation and service limits remain unverified.',
 'tail_rings':'Hard rings from a proposed faunal tail anatomy. Their curved sections are candidates for protective fabrication after inspection; source anatomy, sizing and protective performance remain open.',
 'socket_cups':'Cup-shaped faunal joint parts proposed as small fabrication blanks. Their existing concave form suggests fitted supports or housings, subject to source anatomy, sizing and material suitability.',
 'eye_lenses':'Recovered faunal eye lenses proposed for optical-material investigation. Their clarity and form must be evaluated before any useful viewing element or optical correction is claimed.',
 'filter_plates':'Faunal breathing-filter plates proposed as structured material for dust-exclusion research. Their biological role does not establish safe human filtration or a rated equipment filter.',
 'droppings':'Dried mineral-rich faunal waste proposed for controlled material investigation. Recoverable fractions may have industrial uses, but contamination, composition and safe handling require study; it is not food.',
 'chain':'Corroded lengths of linked metal chain recovered for possible restraint, connection or metal recovery. Each link’s condition and the end attachments must be checked before a load-bearing use is considered.',
 'brass_scrap':'Mixed brass fragments recovered as possible alloy feed. Sorting and inspection are needed before they can enter a controlled reclaiming process; fragment recovery does not establish a specific alloy grade.',
 'bronze_scrap':'Mixed bronze fragments recovered as possible alloy feed. Sorting separates candidate metal from contamination and incompatible pieces before any proposed refining or reuse.',
 'hinge_halves':'Discarded halves of metal hinges, retained as possible fittings or metal feed. A reusable hinge requires a matching half, pin and suitable attachment geometry; an isolated half is not a complete mechanism.',
 'hoop_bands':'Recovered metal bands formed into curved or ring-like sections. They are candidates for retaining or mounting parts after their dimensions, joints and condition have been checked.',
 'pipe_sections':'Sections of old metal pipe recovered as hollow stock. Their bore and wall form suggest sleeves, supports or other fabrication blanks, while corrosion and unknown service history limit direct reuse.',
 'coated_sheet':'Scraps of sheet metal retaining an unknown surface coating. The flat material is candidate fabrication or reclaiming feed; the coating must be identified before heating, bonding or surface treatment.',
 'bent_fasteners':'Bent nails and rivets recovered as small metal scrap. Some pieces may offer recoverable metal, but deformation prevents assuming that they remain suitable fasteners.',
 'furnace_ash':'Coarse residue from a furnace or other controlled hot-work process. It is a candidate source of mineral fractions for investigation, with composition, contamination and useful recovery methods unresolved.',
 'abrasive_fragments':'Fragments of a broken abrasive wheel, retained as possible finishing feed. Useful grains or small working surfaces may remain, but damaged pieces are not a safe replacement for an intact rotating wheel.',
 'magnet_fragments':'Broken pieces of magnetic material recovered from equipment. Remaining magnetism and mechanical condition must be checked before use in small holding or collection tools.',
 'punching_offcuts':'Small metal pieces left by punching operations. They offer shaped fabrication blanks or reclaimable metal depending on size, alloy and condition, without counting as finished fittings.',
}

USES={
 'Machinery repair':'It is intended to replace, support or service a matching machine part; useful fit depends on the host mechanism.',
 'Fabrication':'It provides candidate material or a shaped starting piece for making other objects, with the final form set by the receiving design.',
 'Electrical work':'It belongs in a designed electrical assembly; connections, insulation and a compatible energy supply must be established together.',
 'Survival provision':'It supports preparation of the algae-based supplies on which people depend, with actual reserve use governed by the existing item rules.',
 'Clothing':'Its purpose is to cover the wearer and serve the recorded clothing role, with fit and material constraints set by the item design.',
 'Excavation':'Its working surfaces are intended for breaking or moving suitable ground material; the specific interactions still depend on the tool design.',
 'Resource collection':'It is intended to gather or retain a selected resource so it can be recovered and carried onward.',
 'Traversal':'It is intended to help negotiate terrain, with anchoring, movement and carrying requirements resolved by its eventual equipment design.',
 'Energy storage':'It holds energy in the recorded physical form for a compatible system. Assembly alone does not supply charge, pressure, heat or mechanical preload.',
 'Camp rest':'It supports a physical resting arrangement through shelter, positioning or retention rather than supplying an automatic recovery bonus.',
 'Energy processing':'It is a feed or fitting for the recorded energy-processing chain; the energy source and receiving equipment remain distinct.',
 'Use unresolved':'Its physical form is cataloged for later design, but no qualified practical use is assigned yet.',
 'Metal refining':'It is intended to enter a controlled metal-preparation or refining process; the feed material is distinct from the usable metal eventually produced.',
 'Surface finishing':'It is intended to prepare or change a workpiece’s surface before a later fitting, coating or use; the treatment must suit that material.',
 'Glass and ceramics':'It is intended for a matched glassworking or ceramic process, with composition, forming and controlled heat determining the useful result.',
 'Binding and construction':'It is intended to join, reinforce or support other parts, with the bond or fastening chosen for the materials and loads involved.',
 'Culture support':'It is a proposed input for maintaining a suitable algae culture; the biological requirements and acceptable composition still need verification.',
 'Lubrication':'It is intended to reduce friction at a matched contact surface; dust behavior and material compatibility require qualification.',
 'Electrical insulation':'It is intended to separate conductive parts or support them without a direct conducting path. No voltage or thermal rating follows from that proposed role.',
 'Dust exclusion':'It is intended to obstruct grit at an opening or moving interface. Its fit and wear must be qualified before any exclusion or filtration performance is assigned.',
 'Textile processing':'It is an input to the setting’s difficult biological-material preparation; a useful finished textile is not obtained merely by collecting it.',
 'Tool fabrication':'It supplies a candidate working blank, edge or fitted part for a tool; shaping and mounting depend on the intended job.',
 'Sealing':'It is intended to close a selected joint or patch an interface. Contents compatibility, movement and service conditions must be checked before a seal is relied on.',
 'Protective fabrication':'It provides candidate stock for a protective assembly, with coverage, joining and backing still determining the finished protection.',
 'Fuel':'It is proposed as a material energy input for controlled hot work. The receiving process must establish ignition, handling and a suitable working atmosphere.',
 'Electrical conduction':'It is intended to form part of a continuous conducting path, with section, joints and protection matched to the receiving circuit.',
 'Chemical processing':'It is a candidate industrial input or process aid. Its composition, containment and compatible treatment must be established; usefulness does not imply edibility.',
 'Paper making':'It supplies candidate fibers or prepared feed for husk-based paper, which must still be processed and formed into a usable sheet.',
 'Clothing fabrication':'It supplies a candidate fastening, panel or material for clothing; cutting, joining and fit remain part of the finished garment’s design.',
 'Nutrition':'It is intended for the recorded food or feeding role. Human edibility and useful nutrition require the selected source and preparation rather than assumptions about all fauna.',
 'Fiber processing':'It is intended to assist separation or preparation of biological fibers before they are made into paper or scarce pressed materials.',
 'Equipment repair':'It supports replacement, refitting or maintenance of a compatible item; ownership alone does not restore the equipment’s condition.',
 'Material exclusion':'It is proposed for keeping an unwanted material or organism away from a selected surface, with the target and compatible application still unresolved.',
 'Specialist samples':'It is retained for specialist examination or exchange rather than treated as an ordinary usable supply.',
 'Fauna interaction':'It is intended to influence or manage a selected creature through its recorded mechanism; target species and response require design.',
 'Salvage reuse':'Its former shape may support another use, or it may be stripped for recoverable material after inspection. No specific contents, capacity or recovery yield is assigned.',
 'Liquid and bulk containment':'It is intended to hold the recorded material physically, with closure, compatibility and carrying arrangements resolved for that contents.',
 'Cargo organization':'It is intended to retain or separate physical cargo at its actual location; it does not create remote storage or extra carrying capacity.',
 'Protection':'It is intended to cover or guard the recorded area, with fit, coverage and protective performance still requiring qualification.',
 'Area denial':'It is intended to obstruct movement through a selected space. Placement, triggering and interaction with people, fauna or equipment remain design decisions.',
 'Vision':'It supports the positioning or preparation of a viewing element. Useful clarity, alignment or correction must be established for the actual optics.',
 'Fashion':'Its appearance and workmanship serve the recorded fashion role, independently of armor or special equipment effects.',
 'Sun shading':'It is intended to place a physical screen between the low sun and the user or equipment; orientation, supports and wind retention matter to the final arrangement.',
 'Material transfer':'It provides a proposed means of moving material between compatible parts or vessels while keeping the physical contents accounted for.',
 'Inspection':'It supports looking at or examining a workpiece, with the useful view determined by its optical and mechanical arrangement.',
 'Equipment mounting':'It locates or retains a fitted item on a matching structure; mounting geometry and loads must be chosen together.',
 'Provenance':'It is intended to carry a record of an object’s origin or history; the issuer and meaning of the marks are not yet assigned.',
 'Decoration':'It supplies a visible ornamental surface or detail, with its placement and cultural meaning left to the receiving object’s design.',
 'Devotion':'It is intended for personal or communal devotional use; no particular belief, ritual or supernatural effect is assigned.',
 'Ceremony':'It is intended to be carried, displayed or used in a ceremony. The occasion, ownership and symbolic meaning remain open.',
 'Civic identification':'It is proposed as a visible identifying object, with its issuing community, marks and authority still undefined.',
 'Writing and records':'It provides a physical way to make, carry or preserve marks and records, without assigning a particular script or institution.',
 'Public communication':'It is intended to present a message where others can encounter it; the message and social context remain to be authored.',
 'Civic recognition':'It is intended to represent recognition by a community, with the award, issuer and social meaning still open.',
 'Remembrance':'It is intended as a physical focus for remembering a person or event, without prescribing whose memory it carries.',
 'Food service':'It is intended to portion, hold or serve the recorded food or drink. Food-contact preparation and the contents remain separate design requirements.',
 'Cleaning':'It is intended to dislodge or collect dirt from a suitable surface using its recorded physical form.',
 'Grooming':'It is intended for personal grooming through direct contact, with the working edges and fit shaped for that use.',
 'Trade':'It is a currency entry distinguished by material and workmanship. Issuer, denomination, exchange ratios and particular craft grades remain unassigned.',
 'Atmospheric support':'It is intended for a compatible contained atmospheric-support arrangement; the working medium and service conditions still require research.',
 'Illumination':'It is intended to provide or control a local light source, with fuel or energy supply and operating conditions matched to the design.',
 'Ignition':'It is intended to start a controlled fire or hot-work process using its recorded mechanism; a suitable fuel and working conditions are still required.',
 'Cooking':'It is intended to support controlled preparation or heating of food, with the heat source, vessel and safe ingredients established separately.',
 'Wind power':'It is intended to turn wind-driven motion into useful work for a compatible assembly, with its mounting, load and weather limits unresolved.',
 'Wind propulsion':'It is intended to transmit wind force through a fitted sail or wing arrangement, with the support structure and control method designed together.',
 'Equipment fabrication':'It provides a candidate part for constructing equipment; matching interfaces and final assembly are still required.',
 'Machinery drive':'It is intended to pass or transform motion within a matched mechanism, with the input, supports and driven load designed together.',
 'Food preparation':'It supports a food-designated preparation process. Industrial chemicals and unverified biological fractions cannot substitute for selected culinary inputs.',
 'Magnetic tools':'It is intended for a compatible magnetic holding or collection arrangement. Useful magnetism and mechanical retention must be checked independently.',
 'Music':'Its recorded physical form is intended to produce, control or support sound in a musical instrument; tuning and playable dimensions remain open.',
 'Radio communication':'It is proposed for a matched radio assembly, with detection, transduction, power and performance dependent on further design and qualification.',
 'Offensive combat':'It is intended for the recorded weapon role and compatible ammunition or energy feed. Damage, range, handling and operating requirements remain unassigned.',
 'Magnetic field control':'It belongs to the proposed field-control equipment family; mechanism, affected targets, power supply and useful field behavior remain unresolved.',
 'Static management':'It is intended to manage static charge in a designed arrangement, with the charge path, power and atmospheric behavior still requiring qualification.',
 'Metal forging':'It supports a controlled metalworking operation; the workpiece, heat source and suitable supporting tools must be established for the process.',
 'Fire management':'It is intended to contain, separate or manage heat in a controlled hot-work arrangement, with service conditions matched to its material.',
 'Material analysis':'It supports examination or comparison of a selected material, with sampling and a meaningful interpretation method still required.',
 'Hydration':'It is a proposed drink or drink ingredient within the food-designated Phagia supply. Portion, preparation, preservation and hydration benefit remain undefined.',
 'Defensive combat':'It is intended to interpose a physical guard against a threat, with handling, coverage and protective performance determined by the final design.',
 'Warning and detection':'It is intended to give a detectable warning when its recorded trigger is disturbed; placement, reset and useful warning range remain open.',
}


def description(item):
    key=item['Item ID'];old=str(item.get('Purpose') or '').strip()
    if key in SPECIFIC:return SPECIFIC[key]
    if len(old)>=100:return old
    if key.startswith('proposal.raw.') and key.split('.')[-1] in RAW:
        return RAW[key.split('.')[-1]]
    use=item.get('Primary use') or 'Use unresolved'
    assert use in USES,(key,use)
    generic=old.startswith(('Candidate use:','First-stage material stock for','Material stock for')) or old==item['Item name'] or not old
    if generic:
        name=item['Item name'];material=str(item.get('Material family') or '').lower()
        if item['Category']=='Manufactured stock':
            old=f'Prepared {material} stock in the form of {name.lower()}, intended as an input to further making or assembly.'
        elif item['Category']=='Crafting components':
            old=f'{name}, prepared as a fitted part or intermediate for a larger assembly.'
        else:
            old=f'{name}, retained as a candidate material for {use.lower()}.'
    old=old.rstrip('.;')+'.'
    if item['Category']=='Ancient salvage':
        return old+' It is an extremely rare ancient recovery whose original function remains a working hypothesis. Condition, compatibility and safe operation are unknown; no contemporary reproduction is implied.'
    if use=='Trade':
        return f'{item["Item name"]} is a material-defined currency entry. '+USES[use]
    return old+' '+USES[use]


def prepare():
    assert not AUDIT.exists() or PREVIOUS_AUDIT,'Description completion already recorded; do not replay.'
    OUT.mkdir(parents=True,exist_ok=True)
    original=migration.SOURCE.read_bytes();sheets=parse_sheets(original)
    header=sheets['Items'][4];items=[];changes=[]
    for n,row in enumerate(sheets['Items'][5:],6):
        if not row[0]:continue
        item=dict(zip(header,row));new=description(item)
        assert new and len(new)>70,(item['Item ID'],new)
        if new==item.get('Purpose'):continue
        items.append(dict(id=row[0],name=row[1],previous=item.get('Purpose'),description=new))
        changes.append(dict(sheet='Items',address=f'G{n}',value=new,cachedOnly=False,template=None))
    plan=dict(sourceHash=digest(original),items=items,target='Expanded description',changes=changes,extendTables={},counts={'reviewed':sum(bool(r[0]) for r in sheets['Items'][5:]),'expanded':len(items)})
    (OUT/'before.xlsx').write_bytes(original)
    (OUT/'plan.json').write_text(json.dumps(plan,indent=2),encoding='utf-8')
    shutil.copyfile(migration.ROOT/'Tools/InventoryVisualizer/stage_item_descriptions.mjs',OUT/'stage.mjs')
    print('Reviewed',plan['counts']['reviewed'],'items; expanded',len(items),'descriptions.')


def apply():
    plan=json.loads((OUT/'plan.json').read_text(encoding='utf-8'))
    source=migration.SOURCE;candidate=OUT/'candidate.xlsx'
    candidate.write_bytes((OUT/'before.xlsx').read_bytes())
    migration.OUT=OUT;migration.SOURCE=candidate
    try:migration.apply()
    finally:migration.SOURCE=source
    result=candidate.read_bytes()
    # The adapter already verifies all original values/formulas; additionally prove
    # untouched archive members, cells and edited-cell style attributes verbatim.
    from category_edits import verify_move
    verify_move((OUT/'before.xlsx').read_bytes(),result,{'Items':{c['address']:c['value'] for c in plan['changes']}},category_contract=False)
    assert not AUDIT.exists() or digest(AUDIT.read_bytes())==digest(PREVIOUS_AUDIT)
    guard.release_guard();atomic_write(source,result,plan['sourceHash'])
    audit=dict(date='2026-10-16',authority='Author requested complete descriptions and authorized invention for inadequate entries.',
        basis='Existing master item identity, materials, form, primary use, behavior and limitations; local WORLD_BASIS. Added explanatory prose is local design proposal, not new source-lore fact or implemented mechanics.',
        reviewed=plan['counts']['reviewed'],expanded=len(plan['items']),items=plan['items'],sourceHashBefore=plan['sourceHash'],sourceHashAfter=digest(result),
        scope='Only Items.Purpose descriptions change. Preserve tiers, names, IDs, materials, recipes, source/design status, review/art state and Unity behavior. World identity, streaming/unload, generated-object identity, placement constraints and saved runtime deltas are not applicable because this edits only catalog prose.')
    if PREVIOUS_AUDIT:
        prior=json.loads(PREVIOUS_AUDIT)
        merged={i['id']:i for i in prior['items']}
        for item in audit['items']:
            if item['id'] in merged:item['previous']=merged[item['id']]['previous']
            merged[item['id']]=item
        audit.update(items=list(merged.values()),expanded=len(merged),reviewed=prior['reviewed'],liveItemsAtRefinement=plan['counts']['reviewed'],
                     sourceHashBefore=prior['sourceHashBefore'],
                     passes=[dict(sourceHashBefore=prior['sourceHashBefore'],sourceHashAfter=prior['sourceHashAfter']),
                             dict(sourceHashBefore=plan['sourceHash'],sourceHashAfter=digest(result))],
                     concurrentChanges='Independent live-browser deletions between passes are preserved. Each pass separately verifies that it changes description cells only.')
    atomic_write(AUDIT,(json.dumps(audit,indent=2)+'\n').encode(),digest(PREVIOUS_AUDIT) if PREVIOUS_AUDIT else None)
    print('Saved and verified',len(plan['items']),'descriptions.')


if __name__=='__main__':
    if '--refine' in sys.argv:
        PREVIOUS_AUDIT=AUDIT.read_bytes()
        current={r[0]:r for r in parse_sheets(migration.SOURCE.read_bytes())['Items'][5:] if r[0]}
        for item in json.loads(PREVIOUS_AUDIT)['items']:
            if item['id'] in current:
                assert current[item['id']][6]==item['description'],'A description was changed independently; preserve it and inspect before refining.'
        OUT=OUT/'Refinement';OUT.mkdir(parents=True,exist_ok=True)
        (OUT/'audit-before.json').write_bytes(PREVIOUS_AUDIT)
    guard.OUT=OUT;guard.prepare=prepare;guard.apply=apply;guard.run()
