# CityBuilding — instructions for any AI agent

This project is developed with an AI assistant. Everything the assistant knows about it is kept IN
THIS REPOSITORY, so work can continue with any AI, on any machine, without the original assistant's
private folders.

Read, in this order:
1. `Design/WORKING_RULES.md` — every rule the user set for working on the project (language, approvals,
   builds, tests, commits, data). **Follow it.** Reply to the user in Russian.
2. `CLAUDE.md` — the same rules, short.
3. `.claude/memory/MEMORY.md` — the index of the AI's memory notes: current state, the latest playtest
   lists and their status, the user's decisions, references. Read the notes the task touches.
4. `Design/features/README.md` — the feature index; then ONLY the cards the task touches, then only the
   code files those cards list. Do not read the whole project (~50k lines of C#).

Tools: Unity 6000.5.4f1 (with Android Build Support), Blender 5.2.2, Python 3. Run Unity only through
`Tools/research/run.ps1` (see `Design/features/build-and-test.md`). The balance sheet is a Google Sheet
(id in `Design/WORKING_RULES.md`); editing it needs access to the user's Google account.

After every task: update the feature card(s), the memory notes in `.claude/memory/`, commit, push.
