const shapes={
  'empty':'<path d="M27 20h-7v7m49-7h7v7M20 69v7h7m49-7v7h-7M36 48h24M48 36v24"/>',
  'tool.micro_dust_harvester':'<rect x="31" y="21" width="34" height="58" rx="8"/><path d="M38 21v-7h20v7M31 34h34M31 67h34M43 40h10v21H43zM26 37v24M70 37v24"/>',
  'equipment.algae_canister':'<rect x="32" y="20" width="32" height="60" rx="9"/><path d="M38 20v-6h20v6M32 34h32M32 68h32"/><path class="icon-fill" d="M39 42q10-9 18 0v19H39z"/><path d="m44 58 8-13m-8 5 7 4"/>',
  'equipment.grappling_hook':'<path d="M44 53V24q0-12 12-12t10 12q0 7-7 10M44 34 30 47m14-13 14 13M28 40l2 7 8-1m22-6-2 7-8-1"/><path d="M44 53q-17 1-17 14t21 15q23 0 23-15-1-9-11-9-9 0-9 7 0 6 8 6"/>',
  'equipment.digging_tool':'<path d="m24 76 39-52 6 5-39 52zM50 27l9-12 22 16-10 12zM23 68 11 80l11 8 15-12"/>',
  'equipment.cloak':'<path class="icon-fill" d="M39 18q9-8 18 0l9 17 16 42-17 6-17-6-17 6-17-6 16-42z"/><path d="M39 18q-4 15 9 20 13-5 9-20M48 38v39M30 35l6 38m30-38-6 38"/>',
  'equipment.jacket':'<path class="icon-fill" d="m35 20 13 7 13-7 15 10 9 34-13 5-9-24v36H33V45l-9 24-13-5 9-34z"/><path d="M48 27v54M35 20l5 19 8-12 8 12 5-19M35 57h10m7 0h9"/>',
  'tool.popup_camp':'<path class="icon-fill" d="M10 75 41 21h14l31 54z"/><path d="M41 21 61 75m-40 0 27-36 13 36M8 80h80M53 19l5-7"/>',
  'resource.ironstone_ore':'<path class="icon-fill" d="m17 68 8-31 33-18 24 23-5 32-28 10z"/><path d="m25 37 22 15 11-33m-11 33 35-10m-35 10 2 32m-32-16 30-16 30 22"/>',
  'resource.airborne_dust':'<path d="M16 64q12-13 22 0t22 0 20 0M20 74h58M29 36h1m20-14h1m16 17h1M39 48h1m18 3h1"/><circle cx="28" cy="35" r="3"/><circle cx="50" cy="22" r="2"/><circle cx="67" cy="39" r="3"/>',
  'equipment.arc_battery_empty':'<path class="icon-fill" d="m29 25 9-11h20l9 11v55H29z"/><path d="M29 32h38M38 14v11h20V14M38 42v28m10-28v28m10-28v28M29 75h38"/>',
  'equipment.arc_battery_charged':'<path class="icon-fill" d="m29 25 9-11h20l9 11v55H29z"/><path d="M29 32h38M38 14v11h20V14m-8 24-10 17h12l-9 16M29 75h38"/>'
};
export function itemIcon(id){return `<svg class="item-icon" viewBox="0 0 96 96" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">${shapes[id]||'<path class="icon-fill" d="m24 31 29-14 25 19-7 39-34 8-20-24z"/><path d="m24 31 23 20 31-15M47 51l-10 32m10-32 24 24"/>'}</svg>`;}
