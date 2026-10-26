# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-10-20

### Added

- Standalone payments and billing backend with the `Invoice` and `Payment`
  aggregates, refund records, MediatR commands and queries, EF Core persistence,
  SQLite demo host and platform authorization policies.
- Staff dashboard statistics and revenue buckets, searchable invoice and payment
  ledgers, invoice creation, mark-paid, mark-overdue and void workflows.
- Simulated card, cash and bank-transfer payments with reference idempotency,
  invoice ownership checks and outstanding-balance validation.
- Full and partial payment refunds with remaining-refundable checks.
- Member self-service billing summary, invoice list, invoice detail and invoice
  payment endpoints.
- Angular library with dashboard, invoice ledger and invoice detail components,
  typed HTTP gateway, signal facade, providers and routes.
- Angular demo application with the Kinevor visual system, Sora typography and
  Material icons.
- Flutter package with API client, repository, member invoice ledger, invoice
  detail, payment sheet, theme and strings.
- Flutter example application for Windows and web.
- Real web, mobile and Swagger screenshots under `docs/screenshots`.
- NuGet packing, GitHub Actions CI and tag-based release workflows.
- MIT license and package documentation.
