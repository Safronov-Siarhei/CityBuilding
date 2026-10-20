---
name: feedback-respond-in-russian
description: "User wants all chat responses in Russian, always, in this project"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 04bdd1d4-f9be-4484-b721-4f059c90b34c
  modified: 2026-08-08T16:21:36.509Z
---

Always respond to the user in Russian in this project, in every message, not just when they write in Russian.

**Why:** explicit standing instruction from the user ("на русском всегда пиши!") given after a message where the assistant had switched to English mid-session.

**How to apply:** Default all user-facing text (explanations, status updates, questions) to Russian for this project's conversations, regardless of what language a given tool result, code comment, or file content is in. Code/commit messages/comments can stay in English (existing codebase convention) — this rule is about the chat-facing language, not code.
