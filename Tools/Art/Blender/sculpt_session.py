"""Pen navigation preferences for this Blender process only; never saved globally."""
import bpy

bpy.context.preferences.inputs.use_mouse_emulate_3_button = True
bpy.context.preferences.inputs.mouse_emulate_3_button_modifier = 'ALT'
bpy.context.preferences.inputs.use_rotate_around_active = True
bpy.context.preferences.inputs.tablet_api = 'AUTOMATIC'
bpy.context.preferences.use_preferences_save = False
