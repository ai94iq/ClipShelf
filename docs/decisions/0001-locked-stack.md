# 0001. Locked technology stack

Date: 2026-10-01
Status: Accepted

## Context

The project is built mostly by AI agents for a non-technical owner. Mixing libraries or patterns would make the code harder to maintain and more expensive to change.

## Decision

Use only: .NET 10, WinUI 3 (Windows App SDK 1.x, unpackaged), CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting, SQLite via Microsoft.Data.Sqlite + Dapper with numbered SQL migrations, IMemoryCache, Serilog (File + Async), System.Text.Json, resx localization (English neutral, Arabic satellite), xUnit v3 + NSubstitute, WiX v6 MSI, Central Package Management, .slnx, and .bat scripts.

## Consequences

Any addition or replacement needs a new ADR approved by the owner. Agents propose one option with a reason and wait.
