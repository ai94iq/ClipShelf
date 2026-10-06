# 0005. Defer the caching layer

Date: 2026-10-06
Status: Accepted

## Context

ADR 0001 locked IMemoryCache with `GetOrLoadAsync` as the caching mechanism, and the
template shipped the helper, the DI registration, a size limit and a `cache\` folder.
Nothing ever used them: repositories read SQLite directly, the history is small, and
writes already notify open views through `DataChanged`. The architecture doc described
cache layers that did not exist, and the app created an empty folder at startup.

## Decision

Remove the unused caching layer for now: the helper, the registration, the folder, the
package reference and the documentation claims. When a feature needs caching,
reintroduce IMemoryCache with `GetOrLoadAsync` exactly as ADR 0001 specifies — copy the
helper back from the template or git history — and record the use in the architecture
doc.

## Consequences

- No dead infrastructure and no misleading docs; the app no longer creates `cache\`.
- A future caching need costs one small helper file to restore.
- `Microsoft.Extensions.Caching.Memory` leaves the package list until then.
