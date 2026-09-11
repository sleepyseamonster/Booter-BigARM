# Batch 8 pre-cutover surface and dust baseline

Date: 2026-08-24

This commit protects the existing dirty deposited-dust planner, deposited-dust decorator, and terrain blend shader before Batch 8 moves their inputs behind the World Creator semantic-material authority.

The snapshot preserves user-authored dust wake geometry, clipped overlay mesh behavior, shader texture/transition work, and current local noise-based deposition logic. It is a recovery boundary, not a claim that these files form an independent build: they depend on other user-owned settings and material assets that remain dirty.

Batch 8 may replace input authority and vertex packing while retaining the current shader/material realization as its rollback adapter.
