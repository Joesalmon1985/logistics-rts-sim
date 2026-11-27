# Money and accounting design (owner D6, 27 Nov 2026)

The one monetary and accounting architecture of the island (owner decision **D6.2**). It says what money is in the sim, how it comes to exist, who may create or destroy it, how every payment physically happens, how claims and liabilities are kept, and how organisations and individuals keep double-entry books. Every economic flow of the industrial workstream (refinery sales, electricity bills, wages, taxes, fines, licence fees, auction payments) uses this one system; there is no OpenRA credit balance, no Power Grid money and no external buyer.

**Status: plan only. Nothing in this document is built.** Owner decisions are in [OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md) (D6.2 here; D5.6, D5.16, D5.17, D5.28, D5.33 also apply); the brief is [OWNER_BRIEF_2026-11-27_INDUSTRY.md](../civdiv/OWNER_BRIEF_2026-11-27_INDUSTRY.md) "additional owner decisions" §1 and §3. Cross-package ids, enums, event codes and migrations are fixed by [SHARED_ARCHITECTURAL_CONTRACTS.md](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) (SAC); this document names them and lists what it asks the lead to allocate (section 12). Where it and SAC differ, SAC wins until an amendment row is added.

**Name clash.** "Owner D6" is the owner's decision set of 27 Nov 2026. "Package D6" is the commandments package of SAC. This document always writes "owner D6" or "D6.n" for the decisions and "package D6" for the package.

**Packages.** **K2** (money and accounting core) owns this document. Its Tier 2 minimal part **K2a** is on the industrial vertical slice (D6.18 step 4 and every later payment); **K2b** (institutional account money, deposits, issuance, banking ledger) is Tier 3.

---

## 1. Purpose and scope

**This document owns:**
- what money is in the sim, in three forms that appear in order and never by a switch: barter (no money), commodity money (an accepted good), institutional money (claims on an explicitly defined issuer);
- the issuance and destruction mechanisms of institutional money and who may perform them;
- the `Payment` record and the one payment API: how commodity money moves through the goods API and account money through ledger entries at an issuer;
- double-entry books for organisations (stored) and individuals (derived), in native units, with no imputed prices;
- how claims and liabilities from C2's `FormalObligation` registry enter the books, and the obligation sources the industrial and knowledge packages need;
- the monetary invariants (MON-I1 to MON-I9) and the book invariants (BK-I1 to BK-I4);
- how electricity sales, refinery and resource sales, wages, taxes, fines, licence fees, royalties and auction payments use this one system;
- how the stage 3 trade and victory points relate to it (they are not money; section 10).

**It links to, and does not repeat:**
- goods, lots, holders, owners, reach and the goods API: [SHARED_CONTRACTS.md §2–3](SHARED_CONTRACTS.md#2-inventories-and-goods-movement), [WORLD_ONTOLOGY.md](WORLD_ONTOLOGY.md) (and its §11, the nine categories);
- barter, subjective valuation, price beliefs, `Moneyness`, reference values: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) §3.3, §4.5, §4.11, §12;
- the `FormalObligation` registry (owned by C2), lending, collateral, default: [INSTITUTIONAL_SERVICES_DESIGN.md §5.5](INSTITUTIONAL_SERVICES_DESIGN.md#55-lending-and-deferred-obligations-brief-21); the organisation ledger, offices and powers: [ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md §3.3, §4.6](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md#33-the-organisation-ledger);
- knowledge, designs, patents, licences and the general auction mechanism: [KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md](KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md);
- extraction, refining, factories, vehicles, electricity, waste and industrial military: INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md, VEHICLES_AND_HARVESTING_DESIGN.md, ELECTRICITY_DESIGN.md, WASTE_AND_CIRCULAR_DESIGN.md, INDUSTRIAL_MILITARY_DESIGN.md (the industrial documents; they own their physics and use this document for every payment);
- warehouse tables: [DATA_WAREHOUSE_DESIGN.md §13](DATA_WAREHOUSE_DESIGN.md); KPIs: [KPI_CATALOGUE.md §16](KPI_CATALOGUE.md); acceptance tests: [ACCEPTANCE_TESTS.md D6](ACCEPTANCE_TESTS.md).

## 2. Principles
1. **Production creates goods and services, never money** (D6.2). Extracting ore, hauling it, refining it, generating electricity, delivering anything, completing a factory order, research and construction change matter, energy, knowledge and assets. None of them changes any money balance or creates a `Payment`.
2. **Revenue is a payment by someone who exists.** Income arises only from an actual payment by a customer, purchaser, employer, government, member or other party that is an NPC, a household or an organisation in the world. There is no external buyer, no market sink, no faction treasury that money appears in (owner brief §1: "Remove the previously proposed simplified external buyer").
3. **Money is what is accepted, not what is declared.** Barter is the start. A commodity money is a good that people accept because they believe others will (M5's exchange-use term); nothing flips a "money" flag in the rules. Institutional money exists only when an explicitly defined issuer creates it under its own charter, and it is worth only what holders believe the issuer will honour.
4. **Every unit of money is either a lot or a claim.** Commodity money is lots of a ware, under the goods conservation law C1. Institutional money is a claim on its issuer, under the claims law (every claim has a matching liability). There is no third kind of stock.
5. **A payment is a physical or recorded act by a person.** Commodity money moves only by `Goods.Move` by a body within reach (SAC 7.1: no remote transfer). Account money moves only by a ledger entry at the issuer, authorised by the payer's own action at the issuer's place and written by the issuer's staff. No organisation, contract or policy pays by itself.
6. **Claims never create goods or money.** Opening, defaulting, writing off or assigning an obligation changes no goods count and no money stock (ORG-I2, SVC-I3 kept). Only a payment or a delivery settles it.
7. **Books are double-entry in native units.** Every entry balances in one unit (a ware, or one institutional money). Goods are never valued at an imputed price inside the sim. Reference values (`WareRefValue`, FE) stay analytics only (ECONOMY §12; reflection test extended).
8. **Truth and records stay apart** (D5.28). The payment records and the stored books are truth (layer 1), used for invariants, the warehouse and the god's configured observation. An organisation's decision-makers read only its **records** (layer 4), which its recorder writes and which may be late, incomplete or false.
9. **One system for every ownership model** (D5.17). Private, state, cooperative, communal and religious organisations pay, are paid and keep books the same way. No code branches on `OwnershipModel`.
10. **Determinism.** Payments and billing acts happen inside the acting NPC's intent in tick order; books in ascending `OrgId`; every list bounded and sorted; no floating point; draws (none are needed in K2a) would use `Rng.Fork("money", id)`.

## 3. What money is, stage by stage

| Stage | What exists | How a price or payment looks | How it starts | Package, tier |
|---|---|---|---|---|
| **Barter** (built, M5) | Goods only | `(Ware, Count)` for `(Ware, Count)`, two `ExchangeStep`s of one `DealId` (`Assets/God/Sim/Social/SocialEvents.cs`, codes 210–213), each side valued by its own `Valuation.Marginal` (`Assets/God/Sim/Social/Valuation.cs`) | the start of every scenario | M5, existing |
| **Commodity money** | Goods, one or more of which is commonly accepted in exchange | a payment of `n` units of the accepted ware for goods, labour or a service; recorded as a `Payment` (section 5) | emerges: people accept a ware for its exchange use (price belief `SocialMind.Price`, `Valuation.Observe`); measured by `Moneyness(w)` and `MediumOfExchangeObserved` (ECONOMY 4.11), never decreed. A scenario may **seed** an established convention (section 3.2) | K2a, Tier 2 |
| **Institutional money** | Commodity money plus claims on an issuer that are accepted in payment: deposits (account money), tallies, notes | a payment of `n` units of `MoneyId m`: a ledger transfer between two accounts at the issuer, or the hand-over of note lots | instituted: an organisation founds a money by its own decision under its charter (section 4); people decide whether to accept it | K2b, Tier 3 |

### 3.1 Commodity money
- **It is a good.** Its units are lots of a ware (`ItemLot`, `Assets/God/Sim/Population/PopulationState.cs`), held in carries, on the ground and in bins, owned and moved like any other good, counted by the goods ledger (`GoodsLedger`, same file) under C1.
- **Its quantity changes only as goods change.** Units enter the world only by `Goods.Produce` of that ware through its own physical source or recipe (foraging if it were food, mining and smelting a metal, minting coins from metal, which consumes the metal and labour), and leave only by `Goods.Consume` (wear, loss, melting down as a recipe input, miracle). Producing *another* good never produces it (MON-I4).
- **Which ware.** Any ware may become money; durable, divisible, portable, recognisable wares are more likely to (the mechanics make this possible, they do not choose it). The industrial content may add a coin ware (proposed `Ware.Coin`, made by a mint recipe from a metal; the ware block is the industry addendum's). If none lands, the slice uses an existing durable ware by tuning (`Civ.Money.SliceCommodity`, default `Bronze`, `Assets/God/Sim/Economy/Wares.cs`).
- **Acceptance is belief.** An NPC accepts coins because its price belief for the coin ware (`SocialMind.Price[w]`, `Assets/God/Sim/Social/SocialMind.cs`) makes the exchange worth it by its own valuation. An organisation accepts it because its decision-maker sets a policy (`OrgPolicyKey.AcceptsInPayment`, section 6.4), followed by its staff by their own scores.

### 3.2 A seeded money in the industrial scenario (D6.17)
The industrial starting scenario must not wait for money to emerge. It seeds, as **initial conditions** and never as rules: holdings of the commodity-money ware for persons and organisations; each adult's price belief for it (a convention people already share); and each seeded organisation's unit of account (section 6.4). After tick 0 the same mechanics as everywhere else apply: people may stop accepting it, hoard it, or barter. The seed is listed in the scenario file and in `fact_scenario_seed` (warehouse), so analysis can tell seeded from emerged money.

### 3.3 Institutional money (Tier 3, K2b)
Institutional money is a **claim on an issuer** accepted in payment. Its units are not goods and not lots. Three forms, one ledger:

| Form | What a unit is | Where it is held | How it moves |
|---|---|---|---|
| **Account money** (deposits, tallies) | a claim of the account holder on the issuer, in the issuer's money unit | an `Account` at the issuer (K2 state); the claim is one `FormalObligation` of source `Deposit` per account (debtor: the issuer; creditor: the holder) | a ledger transfer between two accounts at the same issuer (section 5.3) |
| **Notes** (bearer claims) | a physical lot of a note ware that carries a bearer claim on its issuer | carries and bins, like goods; the claim is the issuer's `NotesOutstanding` liability (one bearer obligation per money) | `Goods.Move` like commodity money; whoever owns the lot owns the claim |
| **Interbank and between issuers** | a claim of one issuer on another | an account of one issuer at the other | as account money (Tier 3+, not in the first K2b) |

A tally (ECONOMY §12, "a record of a claim on its store") is the earliest account money: a communal store or temple that records "this person is owed 5 grain" and lets the record be passed to another person in payment has issued account money in grain units. Whether anyone accepts the tally in payment is their own belief about the issuer.

## 4. Issuers, issuance and destruction (who may create money)

### 4.1 Issuers are explicitly defined
```text
MoneyDef                                  // CivState.K2.Monies, ascending Id (K2b)
  Id: MoneyId (from 1, never reused; allocator Money.Found only; counter CivState.K2.NextMoneyId)
  Issuer: OrgId                           // the only organisation whose staff may issue or destroy it
  Form: Account | Note | AccountAndNote
  Redeemable: Ware (or None), RedeemRate (units of ware per unit of money, permille)   // convertibility promise, a liability term
  ReserveRulePermille                     // the issuer's own rule (a charter term), checked by the issuer's staff, not by the sim
  FoundedTick, FoundedByDecision: OrgDecision id
  Status: Active | Suspended | Defunct
```
- **Only an organisation can issue,** and only after its own decision `FoundMoney` under its `DecisionRule` (ORGANISATIONS §4.4), and only if its charter has the purpose `Issue` (a new `Purpose` kind, section 12). Individuals cannot issue money; an individual's IOU is an ordinary obligation.
- **Only an office holder with the `Issue` power** (a new power bit, section 12), acting at the issuer's place (its office node, within `Reach.Touch` of its ledger desk), may record an issuance or destruction. With the office vacant, the issuer issues nothing (SVC-I5 kept).
- Issuers can be any ownership model: a temple's tally store, a cooperative credit society, a private bank, a government treasury or mint.
- **The god issues nothing.** A god owns nothing (SAC G3) and has no account. A miracle may create commodity-money goods (`ProduceReason.Miracle`, with its `ResourceCreated` ledger row, SAC 7.2); it can never create account money or a claim.

### 4.2 The issuance and destruction mechanisms (the complete list)
Each mechanism is one call of `Money.Issue` or `Money.Destroy` (K2b), always paired with the matching asset or liability change so the issuer's books balance. No other code writes an account balance (MON-I1, static test).

| # | Mechanism | Entry at the issuer | Money stock | Who performs it | Notes |
|---|---|---|---|---|---|
| E1 | **Deposit of goods** | Dr Reserves (goods received, native unit memo) / Cr Deposit liability of the depositor | + | depositor hands the goods to the issuer's keeper (`Goods.Move`, basis `Deposited`), keeper records | reserves are real lots in the issuer's bin |
| E2 | **Lending by creating a deposit** | Dr Loan receivable (C2 obligation, source `Loan`) / Cr Deposit liability of the borrower | + | the issuer's officer with `Spend` and `Issue` power, after a loan decision (C8 lending policy) | how banks create money; the loan is one claim, the deposit another, both in the registry |
| E3 | **Issuer's own spending** | Dr Expense (purpose) / Cr Deposit liability of the payee | + | the issuer's officer with `Issue` and `Spend`, under a budget line | only an issuer whose charter allows it (`Charter.MayIssueBySpending`); this is the sovereign-money mechanism; the issuer's net liability rises |
| E4 | **Issuing notes** | Dr Deposit liability of the holder (or Dr Expense) / Cr Notes outstanding; the note lots are produced (`Goods.Produce`, reason `Crafted`, the printing recipe's inputs consumed) | 0 (form change) or + | the issuer's officer at its press | a note lot's existence and the liability are created in the same act |
| D1 | **Withdrawal of reserves** | Dr Deposit liability / Cr Reserves | − | the keeper hands goods to the holder at the issuer's store | fails when reserves are short: a run on the issuer is possible and measured |
| D2 | **Loan repayment from a deposit** | Dr Deposit liability of the borrower / Cr Loan receivable | − | the borrower's authorised act at the issuer (section 5.3) | the C2 obligation is settled by the same act |
| D3 | **Payment to the issuer itself** (taxes, fees, fines owed to the issuer) | Dr Deposit liability of the payer / Cr Revenue (purpose) | − | the payer's authorised act | money paid to its own issuer ceases to exist |
| D4 | **Note redemption or destruction** | Dr Notes outstanding / Cr Reserves or Cr Deposit | − (or form change) | the holder hands the note lots to the issuer, who consumes them (`Goods.Consume`) | |
| D5 | **Write-off on issuer failure** | the claims are written off by C2's `WriteOff` (or a court decision) | − | the issuer's officers or a court, on `Defunct` | holders lose the claim; nothing restores them |

**Not on the list, and therefore impossible:** money created by production, extraction, refining, delivery, electricity generation, a building's completion, a sale to an outside party, a policy, a government "budget", a faction treasury, a victory point, a miracle or the passage of time. Interest (Tier 3) is not issuance: it is an obligation term that accrues a claim (C2 `Accrue`), settled by a payment.

## 5. Payments

### 5.1 The record
```text
Payment                                   // CivState.K2.Payments ring (truth, last PaymentRing entries; history is in events)
  Id: PaymentId (from 1, never reused; allocator Payments.Begin only; counter CivState.K2.NextPaymentId)
  Payer, Payee: Owner                     // Npc, Household or Org (never None, never an outside party: MON-I5)
  Unit: UnitRef { Kind: Ware | Money; Id }  // a ware (commodity money or payment in kind) or a MoneyId
  Amount: int                             // agreed
  Paid: int                               // settled so far (commodity money may take several hand-overs)
  Purpose: PaymentPurpose
  Obligation: ObligationId (0 = spot)     // the C2 obligation this settles, if any
  Deal: DealId (0 = none)                 // the M5 deal whose consideration leg this is, if any
  Ref: int                                // purpose detail: an energy supply contract, a licence RightId, an auction id, a request id, a job contract id
  Authoriser: NpcId                       // who acted for the payer (the payer itself, or an officer or agent with Spend)
  Receiver: NpcId                         // who received for the payee (the payee, or an officer or agent)
  Status: Open | Complete | Failed | Abandoned
  OpenedTick, ClosedTick

enum PaymentPurpose : byte {
  Sale = 1, Wage = 2, Tax = 3, Fine = 4, Fee = 5, LicenceFee = 6, Royalty = 7, AuctionSettlement = 8,
  EnergyBill = 9, Rent = 10, LoanDisbursement = 11, LoanRepayment = 12, Interest = 13, Deposit = 14, Withdrawal = 15,
  Grant = 16, Contribution = 17, Restitution = 18, Damages = 19, Offering = 20, InternalTransfer = 21, Other = 31 }
```

### 5.2 Commodity money and payment in kind: through the goods API
- **The act.** The authoriser (payer or its agent) holds the money lots in its carry, or stands within `Reach.Store` of the payer's bin, and moves `Amount` units to the payee with **`Goods.Move`** (`Assets/God/Sim/Population/Goods.cs`): to the receiver's carry (`MoveReason.HandOver`, within `Reach.HandOver`) or into the payee's bin (`MoveReason.Deposit`, within `Reach.Store`), with `OwnerRule.Set` to the payee (an agent receives for its principal), `purposeRef = PaymentId` and an ownership basis by purpose: `Wage` (16), `Purchase` (23, a sale), `Levy` (19, tax), `Repayment` (18), `Loan` (17), `Restitution` (20), `Offering` (21), `SupplyAgreement` (25, a delivery under an I3 supply agreement, INDUSTRIAL_RESOURCES §4.9), and the proposed generic **`Paid` (26)** for fees, fines, licence fees, royalties, auction settlements, energy bills, rent, grants and damages (section 12).
- **Several steps.** A payment larger than one hand-over (carry limits) completes over several moves; each adds to `Paid`; `PaymentMade` is emitted at completion, `PaymentStep` (HV) for each move. Either side may stop: an abandoned payment leaves the obligation partly settled by exactly what was handed over (SVC-LEND-U1 kept).
- **In kind is the same.** A tax paid in grain, a wage paid in bread, a fuel bill paid in coal: `Unit = Ware(w)` for whatever ware was agreed. Barter is two payments in kind of one deal; K2 records them as `Payment`s only when a party asks for books (an organisation is a party, or a recorded obligation is settled). Person-to-person M5 barter stays as built (exchange events) and is booked by the warehouse, not the sim.
- **Reach is physical.** A refinery cannot pay a harvester owner on the far side of the island: its treasurer (or a courier carrying its coins, owned by the refinery throughout, `OwnerRule.Keep`) must walk there. Remote settlement needs account money (5.3).

### 5.3 Account money: through ledger entries at the issuer (K2b)
- **The act.** The authoriser walks to the issuer's place and gestures a payment order to the issuer's clerk (an office holder with `KeepRecords` at the issuer), or hands the clerk a written order (a document artefact, KNOWLEDGE §6, Tier 3 cheques). The clerk's next act calls **`Payments.Transfer(from: AccountId, to: AccountId, amount, purpose, authoriser, ref)`**: one debit on the payer's deposit, one credit on the payee's, both at the same issuer, in one tick; C2's two `Deposit` obligations change by exactly `amount` (one down, one up) through C2's `Adjust` API.
- **Checks** (the sim's, physical and institutional only): the authoriser is the account holder or holds the `Spend` power of the holding organisation; it is at the issuer's place; the issuer's clerk office is filled and present; the balance covers the amount (no overdraft unless the issuer has granted one as a loan, E2). Whether the clerk *agrees* (it may refuse, misrecord or embezzle) is the clerk's own decision through `IInstitutionPolicy`; the record it writes (layer 4) may differ from the truth transfer only when the clerk lies, which is measured.
- **Different issuers.** A transfer between accounts at different issuers needs interbank claims (Tier 3+). Until then the payer withdraws (D1) and pays in commodity money or notes.

### 5.4 Settlement against obligations
Most industrial payments settle an obligation that exists first:

| Obligation (C2 source) | Created by | Settled by |
|---|---|---|
| `Wage` (exists) | an employment contract's shift worked (C3) | a `Wage` payment |
| `Tax` (exists) | the government's assessment decision (C10) | a `Tax` payment to the government organisation |
| `Fine` (exists), `Restitution` (exists), `Damages` (new) | a court or mediator ruling (C9; KNOWLEDGE §9) | a payment, or seizure by an enforcer (basis `Seized`/`Confiscated`) |
| `Fee` (exists) | a service request accepted with a fee (C6/C7), a patent registration (KNOWLEDGE §8) | a `Fee` payment |
| `Bill` (new) | a billing act by the supplier's treasurer from a meter reading under an electricity or service agreement (ELECTRICITY §4.5 "Bills", `ElectricityBilled`; section 7.1) | an `EnergyBill` (or `Fee`) payment |
| `LicenceFee`, `Royalty` (new) | a licence contract: a one-off fee at grant; royalties billed by the licensor's clerk from the output it knows of (the licensee's report or its own inspection) (KNOWLEDGE §8.4) | a `LicenceFee`/`Royalty` payment |
| `AuctionPrice` (new) | an auction's close with a winner (KNOWLEDGE §11) | an `AuctionSettlement` payment, then the item is transferred |
| `Supply` (new) | each unload under an I3 `SupplyAgreement` (INDUSTRIAL_RESOURCES §4.9: ownership passes at unload with basis `SupplyAgreement` 25 and one obligation opens, debtor buyer, creditor seller, at the agreed price term) | a `Sale` payment (money or goods), linked by I3's `IndustrialSaleClosed` trace |
| `Loan` (exists), `Deposit` (new, K2b) | C2 lending; K2b account opening | repayment; deposits are settled by withdrawal or transfer |

The new sources (`Bill`, `LicenceFee`, `Royalty`, `AuctionPrice`, `Supply`, `Damages`, `Deposit`) and the operations `Adjust`/`Accrue` are requested from C2 (section 12). The principal of an obligation must be able to name a money (`UnitRef`), not only a ware: a K2b amendment to `FormalObligation` (K2a needs only wares).

## 6. Double-entry books

### 6.1 Accounts and units
Books are kept in **native units**: each line has a unit (`UnitRef`: a ware or a money) and a signed quantity; each entry balances **within one unit** (Σ debits = Σ credits per unit). Goods delivered or received are recorded as memo lines in their own ware, never converted at a price.

```text
AccountClass : byte {
  Cash = 1,              // commodity money held, per ware: a flow account (net of payments); the stock is the lots
  Deposit = 2,           // account money held at an issuer (K2b), per MoneyId
  Receivable = 3,        // open obligations where this party is creditor, per unit
  Payable = 4,           // open obligations where this party is debtor, per unit
  Revenue = 5,           // by PaymentPurpose, per unit
  Expense = 6,           // by PaymentPurpose, per unit
  Capital = 7,           // contributions and distributions to owners, per unit
  GoodsIn = 8, GoodsOut = 9,   // memo, per ware: goods received or delivered against a payment or obligation
  Reserves = 10, MoneyIssued = 11, NotesOutstanding = 12   // issuers only (K2b)
}
```

### 6.2 Posting rules (each payment or obligation change posts both parties)

| Event | Payer's or debtor's book | Payee's or creditor's book |
|---|---|---|
| Spot sale of fuel for 12 coins (two steps of one deal) | Dr Expense:Sale 12 coin / Cr Cash 12 coin; memo Dr GoodsIn 5 fuel | Dr Cash 12 coin / Cr Revenue:Sale 12 coin; memo Cr GoodsOut 5 fuel |
| Energy bill issued (40 eu at 1 coin per 10 eu) | Dr Expense:EnergyBill 4 / Cr Payable 4 (coin) | Dr Receivable 4 / Cr Revenue:EnergyBill 4 (coin) |
| Bill paid | Dr Payable 4 / Cr Cash 4 | Dr Cash 4 / Cr Receivable 4 |
| Wage of 3 bread for a shift (in kind) | Dr Expense:Wage 3 bread / Cr Cash 3 bread | (person, derived) Dr Cash 3 bread / Cr Revenue:Wage 3 bread |
| Licence bought by auction for 60 coin | Dr Expense:AuctionSettlement 60 / Cr Cash 60 (the licence is a right, not a booked asset: section 6.5) | Dr Cash 60 / Cr Revenue:AuctionSettlement 60 |
| K2b loan by deposit creation (E2), 100 m | Dr Deposit 100 m / Cr Payable 100 m (borrower) | Dr Receivable 100 m / Cr MoneyIssued 100 m (issuer) |

Revenue and expense are recognised **when obligations open** (accrual) for billed and contracted flows and **when paid** for spot payments; both views are reported (the warehouse keeps `basis` on each line).

### 6.3 Who keeps books
- **Organisations: stored truth books.** `OrgBook` (`CivState.K2.Books`, one per organisation that has made or received a payment or holds an obligation, ascending `OrgId`): a bounded list (≤ 48) of `BookBalance { AccountClass, UnitRef, PurposeOrZero, Dr: long, Cr: long }`, posted only by `Books.Post`, which only `Payments`, `Money` (K2b) and C2's obligation API call. Daily `OrgBookDay` snapshot event (D-level). Stored so the invariants hold across save and load and so the god's observation (under its profile) and the warehouse can show them; **no decision-maker reads them** (reflection test BK-T1).
- **Organisations: records.** What the organisation knows of its finances is its **records** (C6 `RecordEntry`, layer 4): a treasurer with `KeepRecords` writes `PaymentRecorded` entries when it pays or is paid, or learns of a payment by its staff. Embezzlement, forgotten entries and false books are differences between records and truth, measured (INSTITUTIONAL_STATE oversight is kept).
- **Individuals and households: derived books.** People do not keep stored books in the sim. A person's true financial position is computed on demand from truth (`Books.PositionOf(owner)`: lots owned per ware, open claims and liabilities, account balances) and its journal is built by the warehouse from the same events (`fact_journal_line`). What a person *believes* it owes and is owed is its own belief (`CivMind` believed obligations, C2), which may be wrong.

### 6.4 Organisation policies used by K2
- `UnitOfAccount = UnitRef` (default none: books in native units per ware only; scenario seeds may set it). Chosen by the organisation's decision rule; shown in reports as the unit its prices, wages and budgets are stated in.
- `AcceptsInPayment = UnitRef[≤ 3]`: what its staff accept; a staff member offered something else may still accept it by its own scores (policies are followed, not enforced: ORGANISATIONS §4.8).
- `PaysWagesIn`, `TariffPer` (utilities), `PriceList[≤ 8]` (an enterprise's asking prices, `(Ware, Count)` per unit): a policy its sellers follow by their own scores; never binding on buyers; no shared price table (ECONOMY §12).

### 6.5 What the books do not contain
Rights (patents, licences), knowledge, productive assets and goods stocks are not valued in the books. They are listed beside the books (rights held, assets owned, goods by ware) in the organisation card and the warehouse. A licence bought is an expense in the books and a right in the rights registry (KNOWLEDGE §8). Any valuation for analysis uses the warehouse's reference values or observed payment prices, outside the sim.

## 7. How the industrial and institutional flows use this one system

| Flow | Physical side (owning document) | Monetary side (this document) |
|---|---|---|
| **Refinery buys ore** (slice step 4) | the harvester's operator unloads ore into the refinery's input bin (VEHICLES §4.4 `Unload`; under an agreement the owner becomes the refinery with basis `SupplyAgreement` 25, on a spot sale `Purchase` 23) | a `Sale` payment from the refinery to the harvester's owner: the refinery's treasurer or paymaster hands coins to the operator acting as the owner's agent (owner set to the harvester's owner) at the intake; or, under an I3 supply agreement, the unload opens a `Supply` obligation paid later. I3's `IndustrialSaleClosed` links the ownership change and the `PaymentMade`. Alternatives: the refinery owns the mine and the harvester (an internal transfer, no payment), or barters fuel for ore (two payments in kind) |
| **Refinery sells fuel** | fuel delivered by vehicle or collected by the buyer | `Sale` payment, spot or settling the `Supply` obligation of an I3 agreement |
| **Power station obtains fuel** (D6.6) | delivered and burned (ELECTRICITY) | one of: purchase (`Sale`), owned extraction (internal, no payment), supply agreement (a `Supply` obligation per delivery, settled by a `Sale` payment), internal organisational transfer (`InternalTransfer`, a memo between two stores of one org, no money), communal distribution (goods allocated by the commune's ration policy, no payment), government provision (a `Grant` in kind or money from the government org) |
| **Electricity sold** | energy delivered through a connected grid, metered per consumer port (ELECTRICITY §4.6, the energy ledger: not a good, not money) | the electricity agreement (agreed by both parties' office-holders) states a price term; at its billing interval the utility's treasurer issues a bill from the meter reading, which opens one `Bill` obligation (ELECTRICITY §4.5 `ElectricityBilled`); the consumer's authorised member pays at the utility's office (`EnergyBill` payment; `ElectricityPaid` trace). Unpaid bills are arrears; the utility's officers may decide to disconnect (a physical act on the grid). **Generation and delivery post nothing.** Alternatives: prepaid, flat fee, communal utility funded by contributions, public utility funded by government `Grant` payments (ELECTRICITY "agreed funding") |
| **Wages** | shifts worked (C3) | `Wage` obligations per shift, paid by hand-over at the workplace; arrears as today (STATE_DESIGN `Debt`) |
| **Taxes** | none (or goods in kind) | assessed `Tax` obligations (C10 decision), paid at the collector's place or to a collector who visits (`Levy` basis); paying the issuer destroys account money (D3) |
| **Fines, damages, restitution** | enforcement by seizure if unpaid (C9) | obligations from rulings, settled by payment |
| **Licence fees and royalties** | the licensee builds and produces (KNOWLEDGE, industrial docs) | `LicenceFee` at grant; `Royalty` billed by the licensor's clerk from the output it knows of, paid like a bill |
| **Auction payments** | the item is transferred after settlement (KNOWLEDGE §11) | `AuctionPrice` obligation at close; `AuctionSettlement` payment; default voids the sale |
| **Military procurement** | equipment manufactured and delivered (INDUSTRIAL_MILITARY) | `Sale` payments from the military organisation's budget (funded by government `Grant`s or its own revenue); no separate military economy (D6.8) |
| **Waste services** | collection, disposal, recycling (WASTE) | `Fee` payments by those served, or public funding by `Grant`; sale of recyclables as `Sale` |

### 7.1 Billing is an act, never an automatic accrual
No K2 code opens an obligation or posts a revenue line on a timer from truth. A bill (energy, royalty, service fee) exists when the supplier's office-holder performs the billing act from what the organisation knows: its meter reading (a physical dial the reader inspects, ELECTRICITY §4.5–4.6), the licensee's report, or its own inspection. A reader who does not read, or misreads, produces a late or wrong bill; the truth meter is analytics. Obligations with fixed contract schedules (a wage per shift worked, C3; a loan's due day, C2) follow their owners' rules. K2 only posts the books when the obligation opens and when it is paid.

## 8. Invariants

| # | Invariant | Checked |
|---|---|---|
| **MON-I1** No creation outside issuance | For every `MoneyId m`: Σ account balances + notes outstanding = Σ issued(m) − Σ destroyed(m), exactly, at every day boundary and after save/load | state check (K2b) |
| **MON-I2** Payments are double-sided | Every `Payment` debits exactly one payer and credits exactly one payee of equal unit and amount; for a ware unit, the `ItemMoved` events with `PurposeRef = PaymentId` sum to `Paid`, each from a lot owned by the payer to the payee's ownership | events (`PaymentTrace`) |
| **MON-I3** Claims equal liabilities | Per unit, Σ open claims = Σ open liabilities in the C2 registry (deposits included); every `Receivable` balance of an org book equals its open obligations as creditor, every `Payable` its open obligations as debtor | state check |
| **MON-I4** Production creates no money | No `Produced`, extraction, refining, `ItemMoved(Deliver)`, production-cycle or energy-generation event changes any `Payment`, account or book; a commodity-money ware is produced only by its own source or recipe. Static test: only `Payments`, `Money` and `Books` write `CivState.K2`; no industrial module references them except to call `Payments.Begin/Step` from an NPC's act | static + events |
| **MON-I5** No outside party | Every `Payment`'s payer and payee is an existing Npc, Household or Org (`OwnerKind.None`, `Group`, `Estate` excluded except estate settlement by its executor); no type named or acting as an external market, buyer or sink exists in `Sim/**` (static test `NoExternalBuyer`) | static + state |
| **MON-I6** Reference values are analytics only | `WareRefValue`, FE and any money valuation are referenced only in `Telemetry`, the runner and report code (reflection test of ECONOMY §12 extended to `Sim/Civ/Money*`) | reflection |
| **MON-I7** Remote payment impossible | Every commodity-money step passed `Goods.Move`'s reach check (C6); every account transfer has an authoriser at the issuer's place and a clerk present (from the `PaymentMade` node fields) | events |
| **MON-I8** Settlement only by payment or delivery | An obligation's settled amount changes only by `PaymentMade`/`PaymentStep` (money) or a delivery `ItemMoved` (an obligation payable in goods), plus write-off, forfeiture and court decision events | events |
| **MON-I9** God owns no money | No account, payment or book has a god as party; miracles never touch `CivState.K2` | static |
| **BK-I1** Entries balance | Per org book, per unit: Σ Dr = Σ Cr | state check |
| **BK-I2** Books equal payments | Per org, Revenue and Expense by purpose equal Σ completed payments (and opened obligations, accrual basis) of that purpose | events |
| **BK-I3** Cash flow identity | Per org per commodity unit per day: Δ lots owned = Cash net (payments in − out) + non-payment goods flows of that ware (produced, consumed, moved without a `PurposeRef` payment), exactly | warehouse (W-D6-3) |
| **BK-I4** Records are not truth | No `IInstitutionPolicy`, `IFactionPolicy`, `INpcPolicy` or `IGodPolicy` type references `OrgBook` or `Payment` (reflection) | reflection |

## 9. Interactions with other systems
- **M5 exchange.** A sale between persons stays an M5 deal (offer, accept, two hand-overs, either can defect). K2 adds the `Payment` record when one side is an organisation, a recorded obligation is settled, or the commodity-money convention is on (tuning `Civ.Money.RecordPersonSales`, default 1 in the industrial scenario, 0 elsewhere for neutrality). Price beliefs (`Valuation.Observe`) learn from money payments exactly as from barter (the money ware is a ware).
- **Learning.** The goods reward stays the only goods channel into learning (SAC; `GoodsWorth.Of`). Receiving coins rewards through the money ware's believed worth like any ware; no extra "profit" reward exists.
- **C1** provides `Civ.Enabled`, productive assets, `Deliver` hauling and theft classification (stealing coins is theft); **C2** the obligation registry and its API; **C3** wages (its `Pay` verb 101 becomes the general payment act, section 12); **C6** organisations, offices, powers, records and policies; **C10** taxes, government decisions, legal-tender policies; **C9** fines and damages; **C8** (Tier 3) banking services on the K2b ledger.
- **Divinity.** A god can suggest, command or compel a treasurer to pay, possess a clerk, or create coin goods by miracle (booked in the resource ledger). It cannot issue account money, write a ledger entry or pay from nothing. A compelled payment is recorded with `control_code` 1 like any compelled act.
- **Death and estates.** Money lots go into the estate like any goods (M4). A deceased person's deposit becomes a claim of the estate (creditor `Estate(x)`) until the executor transfers it (K2b). Resurrection does not restore paid-out estates (D5.13).

## 10. Stage 3: no credits, and how it maps (temporary mechanism)
Checked in code: stage 3 has **no money or credits**. Victory points are a territorial score (`FactionRules.VictoryPoints`, `Assets/God/Sim/Factions/FactionRules.cs`: HQ and citadel counts), never spent or traded. Faction trade is barter between HQ stocks (`EcoBuilding.Stock`, `Assets/God/Sim/Economy/EconomyState.cs`) carried by envoys (`TradeOffer.Give/Want` as `WareCount[]`, `Envoy.Cargo`, `Assets/God/Sim/Trade/TradeState.cs`), judged with the faction policy's own table `TradeTuning.ValueOf` (`Assets/God/Sim/Trade/TradeTuning.cs`), with its own ledger (`TradeState.Departed/Arrived/Lost`).

Mapping to D6.2: the stage 3 mode remains the listed temporary mechanism (DEV_LOG); its envoy trade is **payment in kind between two government stocks**, consistent with D6.2 (nothing is created, the trade ledger conserves wares). K2 does not post it (HQ stocks are outside the personal goods ledger until M6), never converts victory points or `ValueOf` figures into money, and the industrial workstream never uses stage 3 stocks. The OpenRA "ore for credits" buyer of the first brief is **not implemented anywhere and must not be**. If an industrial test needs a buyer before the refinery has customers, the scenario seeds a real buyer organisation with real holdings (a party in the world, MON-I5); any such seed is listed in the DEV_LOG temporary-mechanisms table.

## 11. Player and view
- **In the world:** coins change hands visibly (the hand-over animation with the coin ware), a paymaster walks to the harvester, a customer queues at the utility office. A payment is never a floating number.
- **Organisation card:** revenue and expense by purpose and unit, receivables, payables, arrears, accepted units, unit of account, **as the organisation's records state them**, beside the truth figures that the god's observation profile allows (D5.28); never mixed.
- **NPC inspector:** the person's money holdings (lots), the obligations it believes it has, payments it made and received.
- **Economy overlay:** flows of payments between organisations (from events), money stock by holder class, the moneyness of wares (analytics).

## 12. Contract requests to the lead (for the SAC Industry addendum)

| # | Request | Why | Neutral? |
|---|---|---|---|
| K2-R1 | `CivState.K2` sub-object (partial `Civ/CivState.K2.cs`), hashed after C5 in the `civ` part only when non-null; switch `Civ.Money.Enabled` (default 0) | state slot | yes (null when off) |
| K2-R2 | Id spaces `PaymentId` (`NextPaymentId`), `MoneyId`, `AccountId` (K2b) in `CivState.K2` | identity (SAC §1 rules) | yes |
| K2-R3 | `OwnershipBasis.Paid = 26` (generic payment basis; 25 is I3's `SupplyAgreement`); keep `Purchase 23`, `Wage 16`, `Levy 19`, `Repayment 18`, `Loan 17` | basis per purpose | yes (unused until written) |
| K2-R4 | C2 amendments: obligation sources `Bill`, `LicenceFee`, `Royalty`, `AuctionPrice`, `Supply`, `Damages`, `Deposit`; operations `Adjust` (K2b account transfers) and `Accrue` (interest, Tier 3); a call to `Books.Post` on open, adjust and close; principal as `UnitRef` (K2b) | settlement and books | yes while K2 is off |
| K2-R5 | C6 amendments: `Purpose.Issue`, `Purpose.Lend` (exists), power bit `Issue`, office kind `Clerk` (or `Recorder` reused), policies `UnitOfAccount`, `AcceptsInPayment`, `PaysWagesIn`, `TariffPer`, `PriceList`; charter term `MayIssueBySpending` | issuers and policies | yes |
| K2-R6 | C3's verb 101 `Pay` is the general payment act (any `PaymentPurpose`), with the `Payment` record created by `Payments.Begin` | one payment act | coordinate with C3 |
| K2-R7 | Domain `Money = 79` (Industry has 70–75, K1 76–78; `RangeOf` → 1100–1199, shared with K1) and event codes (proposal) **1170–1189**: 1170 `PaymentMade`, 1171 `PaymentStep` (HV), 1172 `PaymentFailed`, 1173 `PaymentAbandoned`, 1174 `OrgBookDay` (D), 1175 `UnitOfAccountChanged`, 1176 `ObligationAccrued` (if C2 does not own it), 1177 `MediumOfExchangeObserved` (moved from ECONOMY's plan); K2b: 1180 `MoneyFounded`, 1181 `MoneyIssued`, 1182 `MoneyDestroyed`, 1183 `AccountOpened`, 1184 `AccountClosed`, 1185 `AccountTransferred`, 1186 `DepositAccepted`, 1187 `WithdrawalPaid`, 1188 `NotesIssued`, 1189 `NotesRedeemed` | events | catalogue rows only |
| K2-R8 | Migrations `0038_money_claims.sql` (K2a) and `0042_monetary_institutions.sql` (K2b) (the I packages take 0031–0037) (DATA_WAREHOUSE §13.4) | warehouse | recording only |
| K2-R9 | Optional ware `Coin` (proposed 65, from the spare 63–71 of the industrial ware block, INDUSTRIAL_RESOURCES §3.1), with a mint recipe (`ProduceReason.Processed`); else `Civ.Money.SliceCommodity` names an existing ware | commodity money for the slice | yes |

## 13. Tier and package mapping

| Capability | Tier | Package | Minimal first version |
|---|---|---|---|
| Barter | built | M5 | unchanged |
| Commodity money, `Payment` record, payment act through the goods API, `PaymentMade` | 2 (slice) | **K2a** | coin or seeded ware; person and org payments; spot and obligation settlement |
| Org double-entry books (`OrgBook`), posting rules, BK-I1..I4, records written by treasurers | 2 (slice) | **K2a** (+ C6 records) | balances per (class, unit, purpose); daily snapshot |
| Books for billing acts (energy bills, royalties) | 2 (slice) | K2a with C2 and I4 | the slice factory's one energy bill (ELECTRICITY "one bill as an obligation") |
| Unit of account, accepted units, price lists | 2 | K2a | policies only |
| Derived person books, journal lines | 2 | K2a (warehouse) | `fact_journal_line` |
| Moneyness measure and `MediumOfExchangeObserved` | 2 | K2a (analytics) | as ECONOMY 4.11 |
| Institutional money: issuers, accounts, issuance E1–E4 and destruction D1–D5, account transfers | 3 | **K2b** (+ C8 services) | a tally store: deposits of grain, transfers at the store |
| Notes, interest, interbank claims, runs, monetary policy | 3 | K2b, C8 | later |

## 14. Tests and acceptance hooks

**Invariants (every build, EM and RT):** MON-INV-1 to MON-INV-9 and BK-INV-1 to BK-INV-4 check the invariants of section 8 over the industrial scenario, seeds 1–3 × 10 days, and the slice fixture (exact).

**Unit tests (EM):**
- **MON-U1** a spot sale of 5 fuel for 12 coin between two organisations via their agents: lots move with `PurposeRef = PaymentId`, owners change to the right parties, both books post the table 6.2 lines, C1 holds, no money stock changes.
- **MON-U2** a harvester unloading 20 ore at a refinery with **no buyer agreement** changes no book, creates no payment and no money (the ore's ownership follows the delivery contract or stays with the harvester's owner).
- **MON-U3** a power station generating for 100 intervals into a grid with a connected factory changes no book, payment or money stock; the utility treasurer's billing act then opens exactly one `Bill` of (meter delta × price term) and posts both books; with no billing act, nothing is owed.
- **MON-U4** a payment attempted beyond reach fails with `OutOfReach` and changes nothing (atomic).
- **MON-U5** an abandoned two-step payment leaves the obligation reduced by exactly what was handed over.
- **MON-U6** (K2b) E1–E4 and D1–D5 each change the money stock by exactly the table's sign and amount; an account transfer with the clerk office vacant is refused.
- **MON-U7** static `NoExternalBuyer`: no type in `Sim/**` creates a payment whose payer or payee is not an existing party; `Payments`, `Money`, `Books` are the only writers of `CivState.K2`.

**Acceptance hooks** ([ACCEPTANCE_TESTS.md D6](ACCEPTANCE_TESTS.md)): D6.1 (oil without money), D6.2 (sale through payment and ownership transfer), D6.3 (electricity without money), D6.7 (licence purchase), the slice step VS4 and VS10 reconciliation.

## 15. Open questions (each with the default used)
1. **Do person-to-person M5 trades get `Payment` records?** Default: only in the industrial scenario (`RecordPersonSales = 1`), so existing hashes are unchanged; the warehouse books M5 trades from exchange events everywhere.
2. **Accrual or cash basis in reports?** Default: both kept (line `basis`), reports show cash first.
3. **Should the sim block a payment the payer's own records say it cannot afford?** Default: no; only the physical check (it holds the lots) and the issuer's balance check apply.
4. **Who meters royalties when the licensor cannot see production?** Default: the licensor bills from the licensee's report or its own inspection (section 7.1); under-reporting is possible and measured against the truth count (analytics); an audit service (Tier 3) closes the gap.
