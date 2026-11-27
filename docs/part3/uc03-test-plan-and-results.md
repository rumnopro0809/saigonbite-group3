# Part 3 QA & Testing Report — UC03 Hubs, Rain Flag, HT03 Kitchen/Shipper and R2

**Author:** Lê Quang Tấn (`524H0188`) | **Branch tested:** `main` + UI commits (`d8d1116`) | **Date:** 28 November 2026
**Target Stack:** ASP.NET MVC 5 (`.NET Framework 4.7.2`), Entity Framework 6.4.4, SQL Server Express (`.\SQLEXPRESS`), Bootstrap 5
**Test Framework:** MSTest 2.2.10, run with `vstest.console.exe` (VS 2022 Build Tools)

---

## 3.4.1 Test Plan and Execution Summary

### Scope and approach
- **Unit and integration tests (automated):** 75 MSTest cases in five classes: `HubServiceTests`, `RainSettingServiceTests`, `HubReportServiceTests`, `KitchenBoardServiceTests` and `ShipperServiceTests`. They call the services directly against a real SQL Server Express database (`SaigonBiteTests`, separate from the demo database `SaigonBiteDb`), so the EF6 mapping, transactions and `RowVersion` checks are exercised, not mocked.
- **Isolation:** each test creates its own hubs, brands, users and orders through `Uc03Fixture` (`Uc03TestData.cs`) and deletes them in `Dispose`, so the seeded data (3 hubs) stays unchanged and `DatabaseSeedTests` still passes.
- **Techniques:** equivalence partitioning and boundary values for capacity and load thresholds; state-transition testing for the kitchen and shipper lifecycle (valid and invalid transitions); negative authorization tests (wrong role, wrong hub, wrong shipper); and concurrency tests with stale, missing and malformed `RowVersion` values.
- **Manual tests:** role access and the end-to-end flow in the browser on IIS Express (section 3.4.3).

### Result
| Suite | Tests | Passed | Failed |
| :--- | :---: | :---: | :---: |
| UC03/HT03 classes (this report) | 75 | 75 | 0 |
| Whole solution (UC01 + UC02 + UC03) | 170 | 170 | 0 |

Build: `MSBuild SaigonBite.sln` with 0 errors. Run on 28 November 2026.

---

## 3.4.2 Automated Test Cases

### A. FR14.1 Create hub (`HubServiceTests`)
| Test ID | FR / BR | MSTest Method | Input | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- | :---: |
| TC14.1-1 | FR14.1 | `Create_PersistsHubAndBrandLinksAtomicallyAndReturnsCreated` | Valid name, address, capacity and brand list | Hub and `HubBrand` rows saved in one transaction; `Created` returned | Pass |
| TC14.1-2 | FR14.1, BR8 | `Create_RejectsZeroAndNegativeCapacityAndWritesNothing` | Capacity `0` and `-1` | `Invalid` on `Capacity`; no hub or brand link written | Pass |
| TC14.1-3 | FR14.1, BR8 | `Create_AcceptsCapacityOne` | Capacity `1` (lowest valid value) | Hub created with `MaxCapacity = 1` | Pass |
| TC14.1-4 | FR14.1 | `Create_RejectsBlankOrOverlongNameAndLocation_AndAcceptsMaximumLengths` | Blank, max-length and max+1 name and address | Blank and too long rejected; exact maximum accepted | Pass |
| TC14.1-5 | FR14.1 | `Create_RejectsDuplicateUnknownAndSoftDeletedBrandsWithoutPartialHub` | Duplicate, unknown or soft-deleted brand ID | Rejected; no partial hub saved | Pass |

### B. FR14.2 View hub and load (`HubServiceTests`)
| Test ID | FR / BR | MSTest Method | Input | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- | :---: |
| TC14.2-1 | FR14.2, FR02, BR7, BR8 | `ListAndGet_LoadCountsOnlyConfirmedAndPreparing_AndClassifiesThresholds` | Capacity 10; Placed, Ready, Cancelled, OutForDelivery, Delivered and Failed orders, then 2, 3, 8 and 9 Confirmed/Preparing orders | Non-counting statuses give 0%; 20% Quiet; **30% Normal**; 80% Normal; **90% Full**; list and details show the same value | Pass |
| TC02-3 | FR02, BR8 | `Quote_AcceptsHubAtEightyPercentAndRejectsAtNinetyPercent` | Capacity 10, 4 Confirmed + 4 Preparing plus 9 non-counting orders, then 1 more Confirmed | UC01 quote load `0.8` accepted; at 90% the quote is rejected with `BR8` | Pass |

### C. FR14.3 Update hub and concurrency (`HubServiceTests`)
| Test ID | FR / BR | MSTest Method | Input | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- | :---: |
| TC14.3-1 | FR14.3, BR17 | `Update_ChangesFieldsAndBrands_AndKeepsExistingOrdersAndSnapshots` | New name, address, capacity and brands | Hub updated; existing orders and their price snapshots unchanged | Pass |
| TC14.3-2 | FR14.3, BR8 | `Update_LoweringCapacityBelowActiveOrdersGivesLoadAboveFullWithoutChangingOrderStatus` | Capacity lowered below the number of active orders | Load shows above 100%; no order status rewritten | Pass |
| TC14.3-3 | FR14.3 | `Update_RejectsInvalidInputAndMissingHub_ChangingNothing` | Invalid fields; unknown hub ID | `Invalid` / `NotFound`; database unchanged | Pass |
| TC14.3-4 | FR14.3 | `StaleRowVersion_IsRejectedForUpdateSetActiveAndDelete_AndChangesNothing` | Old `RowVersion` after another save | `Conflict` for update, set-active and delete; nothing changed | Pass |
| TC14.3-5 | FR14.3 | `MissingOrMalformedRowVersion_IsRejectedForUpdateSetActiveAndDelete_AndChangesNothing` | Empty or non-Base64 `RowVersion` | Rejected; nothing changed | Pass |

### D. FR14.4 Stop orders and soft-delete (`HubServiceTests`)
| Test ID | FR / BR | MSTest Method | Input | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- | :---: |
| TC14.4-1 | FR14.4 | `SetActive_False_KeepsHubAndExistingOrders_AndCanBeReactivated` | Deactivate, then reactivate | Hub and its orders kept; reactivation works | Pass |
| TC14.4-2 | FR14.4 | `Delete_SoftDeletesHubKeepingRowOrdersAndHistory_AndSecondDeleteIsNotFound` | Delete a hub that has orders and history | `IsDeleted = true`; row, orders and history kept; second delete returns `NotFound` | Pass |
| TC14.4-3 | FR14.4, FR01 | `Quote_RejectsNewOrderForHubDeactivatedThroughHubService_AndAcceptsAgainAfterReactivation` | UC01 quote on a deactivated hub, then after reactivation | Rejected (`NOT_FOUND`), then accepted | Pass |
| TC14.4-4 | FR14.4, FR01 | `Quote_RejectsNewOrderForSoftDeletedHub` | UC01 quote on a soft-deleted hub | Rejected | Pass |

### E. FR16 Heavy-rain flag (`RainSettingServiceTests`)
| Test ID | FR / BR | MSTest Method | Input | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- | :---: |
| TC16-1 | FR16 | `Get_CreatesDefaultOffRowWhenMissing_AndReturnsExistingOtherwise` | No `SystemSetting` row; then an existing row | Default row with the flag off is created; existing row returned | Pass |
| TC16-2 | FR16, BR5 | `Set_TogglesFlagWithValidRowVersion_AndPersistsIt` | Turn on and off with the current `RowVersion` | Flag saved | Pass |
| TC16-3 | FR16 | `Set_RejectsStaleRowVersionWithConflict_AndKeepsFlag` | Old `RowVersion` | `Conflict`; flag unchanged | Pass |
| TC16-4 | FR16 | `Set_RejectsMissingOrMalformedRowVersion_AndKeepsFlag` | Empty or invalid `RowVersion` | Rejected; flag unchanged | Pass |
| TC16-5 | FR16, BR5, BR17 | `ExistingOrderKeepsHeavyRainSnapshotAfterFlagChanges_WhileNewQuoteUsesNewFlag_TC16` | Place an order, then flip the flag | Old order keeps `HeavyRainSnapshot`; new quote uses the new flag | Pass |

### F. FR19 R2 Hub Utilization (`HubReportServiceTests`)
| Test ID | FR / BR | MSTest Method | Input | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- | :---: |
| TC19-1 | FR19 | `Build_RejectsFromAfterTo` | `from` later than `to` | Validation error | Pass |
| TC19-2 | FR19 | `Build_AcceptsSingleDayRange_AndKeepsReportDates` | `from = to` | Accepted; dates kept as entered | Pass |
| TC19-3 | FR19, AMB06 | `Build_HandledCountUsesVietnamTimeBoundaries` | Orders confirmed near midnight (UTC+7) | Counted on the correct Vietnam date | Pass |
| TC19-4 | FR19, AMB06 | `Build_HandledCountIncludesLaterCancelledOrders_AndCountsEachOrderOnce` | Order Confirmed and later Cancelled | Counted once as handled | Pass |
| TC19-5 | FR19, AMB06 | `Build_AveragePrepMinutesUsesOnlyOrdersWithBothConfirmedAndReady` | Mix of complete and incomplete Confirmed→Ready pairs | Mean uses complete pairs only | Pass |
| TC19-6 | FR19, AMB06 | `Build_OrderConfirmedOnLastDayAndReadyAfterMidnightStillHasPrepSample` | Confirmed on the last day, Ready after midnight | Sample still counted | Pass |
| TC19-7 | FR19 | `Build_WithoutAnyCompleteSample_AveragePrepIsNullAndSampleCountZero` | No complete pair | Mean is `null` ("no data"), not 0 | Pass |
| TC19-8 | FR19, BR7 | `Build_QuietKitchenCountComesFromOrderSnapshotIncludingLaterCancelledOrders` | Orders with the Quiet Kitchen discount in their snapshot, one later cancelled | All of them counted | Pass |
| TC19-9 | FR19, BR8 | `Build_CurrentLoadMatchesHubServiceLoad_TC19` | Hub with active orders | R2 current load equals the hub list's load | Pass |
| TC19-10 | FR19, FR14.4 | `Build_OmitsSoftDeletedHubs` | One soft-deleted hub | Not shown in the report | Pass |

### G. FR17 Kitchen board (`KitchenBoardServiceTests`)
| Test ID | FR / BR | MSTest Method | Expected Result | Status |
| :--- | :--- | :--- | :--- | :---: |
| TC17K-1 | FR17 | `GetBoard_ReturnsOnlyOwnHubActiveOrdersInFifoOrderWithLines` | Only the staff member's own hub, oldest first, with order lines | Pass |
| TC17K-2 | FR17, BR8 | `GetBoard_Load_CountsOnlyConfirmedAndPreparing` | Board load follows BR8 | Pass |
| TC17K-3 | FR17 | `GetBoard_EmptyHub_ReturnsEmptyBoardWithZeroLoad` | Empty board, 0% load | Pass |
| TC17K-4 | FR17, NFR04 | `GetBoard_NonKitchenOrInvalidUsers_ThrowUnauthorized` | Non-kitchen users rejected | Pass |
| TC17K-5 | FR17, NFR04 | `Act_NonKitchenOrInvalidUsers_ThrowUnauthorizedAndLeaveOrderUnchanged` | Rejected; order unchanged | Pass |
| TC17K-6 | FR17, NFR06 | `Act_HappyPath_PlacedToConfirmedToPreparingToReady_WritesHistoryAndRaisesLoad` | Placed→Confirmed→Preparing→Ready with history rows | Pass |
| TC17K-7 | FR17, BR15 | `Act_ConfirmOrderOfAnotherHub_ThrowsBR15AndLeavesOrderUnchanged` | Other hub's order rejected | Pass |
| TC17K-8 | BR15 | `Act_StartPreparingOnPlaced_ThrowsBR15WithoutHistory` | Placed→Preparing rejected | Pass |
| TC17K-9 | BR15 | `Act_MarkReadyOnConfirmed_ThrowsBR15WithoutHistory` | Confirmed→Ready rejected | Pass |
| TC17K-10 | BR15 | `Act_ConfirmOnPreparing_ThrowsBR15WithoutHistory` | Confirm on a Preparing order rejected | Pass |
| TC17K-11 | BR15 | `Act_ActionOnDeliveredOrder_ThrowsBR15WithoutHistory` | Terminal state cannot change | Pass |
| TC17K-12 | BR15, BR16 | `Act_RejectWithoutReason_ThrowsBR15AndLeavesOrderUnchanged` | Reject without a reason refused | Pass |
| TC17K-13 | BR16 | `Act_RejectPlacedPaidMoMoOrder_CancelsRefundsAndRecordsReason` | Cancelled, simulated refund, reason saved | Pass |
| TC17K-14 | BR16 | `Act_RejectConfirmedOrder_ThrowsBR15AndKeepsPayment` | Reject after Confirmed refused | Pass |
| TC17K-15 | NFR05 | `Act_StaleRowVersion_ThrowsConcurrencyAndChangesNothing` | Stale page rejected | Pass |
| TC17K-16 | FR17 | `Act_UnknownOrder_ThrowsNotFound` | Unknown order returns `NotFound` | Pass |

### H. FR17 Dispatch and shipper board (`ShipperServiceTests`)
| Test ID | FR / BR | MSTest Method | Expected Result | Status |
| :--- | :--- | :--- | :--- | :---: |
| TC17S-1 | FR17, AMB01 | `ListShippers_ReturnsOnlyActiveNotDeletedShippers` | Only active shippers listed | Pass |
| TC17S-2 | AMB01 | `AssignShipper_ReadyOrder_SetsShipperIdAndKeepsStatus` | `ShipperId` set; status stays Ready | Pass |
| TC17S-3 | AMB01 | `AssignShipper_AlreadyAssignedReadyOrder_CanBeReassigned` | Reassignment before pickup allowed | Pass |
| TC17S-4 | AMB01, BR15 | `AssignShipper_NonReadyOrders_ThrowBr15AndKeepShipperUnchanged` | Only Ready orders can be assigned | Pass |
| TC17S-5 | FR17 | `AssignShipper_UnknownOrder_ThrowsNotFound` | `NotFound` | Pass |
| TC17S-6 | NFR04 | `AssignShipper_NonAdminCallers_ThrowUnauthorizedAndKeepShipperUnchanged` | Only Admin can assign | Pass |
| TC17S-7 | AMB01 | `AssignShipper_TargetNotActiveShipper_ThrowsShipperNotFound` | Inactive or non-shipper target rejected | Pass |
| TC17S-8 | NFR05 | `AssignShipper_StaleRowVersion_ThrowsConcurrencyAndKeepsFirstAssignment` | Two Admins at once: first wins | Pass |
| TC17S-9 | FR17 | `ReadyOrders_ReturnsOnlyReadyOrdersOldestFirstWithShipperName` | Dispatch list: Ready only, oldest first | Pass |
| TC17S-10 | NFR04 | `ReadyOrders_NonAdminCallers_ThrowUnauthorized` | Non-Admin rejected | Pass |
| TC17S-11 | FR17, NFR04 | `GetMyOrders_ReturnsOnlyOwnReadyAndOutForDeliveryOrdersWithLines` | Shipper sees only their own orders | Pass |
| TC17S-12 | NFR04 | `GetMyOrders_NonShipperCallers_ThrowUnauthorized` | Non-shipper rejected | Pass |
| TC17S-13 | NFR04 | `Act_NonShipperCallers_ThrowUnauthorizedAndLeaveOrderUnchanged` | Rejected; order unchanged | Pass |
| TC17S-14 | FR17, NFR06 | `Act_PickUpAssignedReadyOrder_MovesToOutForDeliveryAndWritesHistory` | Ready→OutForDelivery with history | Pass |
| TC17S-15 | BR15 | `Act_PickUpUnassignedOrAnotherShippersOrder_ThrowsBr15AndLeavesOrderUnchanged` | Cannot pick up others' orders | Pass |
| TC17S-16 | BR15 | `Act_PickUpNonReadyOrders_ThrowsBr15AndLeavesOrderUnchanged` | Only Ready can be picked up | Pass |
| TC17S-17 | BR14, BR15 | `Act_DeliverCodWithoutCashCollected_ThrowsBr15AndRecordsNoPayment` | COD without "cash collected" refused | Pass |
| TC17S-18 | BR14 | `Act_DeliverCodWithCashCollected_DeliversMarksPaidAndRecordsCodCollection` | Delivered; payment marked paid | Pass |
| TC17S-19 | BR14 | `Act_DeliverOnlinePaidOrderWithoutCashCollected_Delivers` | Online order needs no cash tick | Pass |
| TC17S-20 | BR15 | `Act_DeliverAnotherShippersOrder_ThrowsBr15AndLeavesOrderUnchanged` | Refused | Pass |
| TC17S-21 | FR17, BR15 | `Act_FailWithNullOrBlankReason_ThrowsBr15AndLeavesOrderUnchanged` | Failed needs a reason | Pass |
| TC17S-22 | FR17 | `Act_FailWithReason_MarksFailedAndStoresTrimmedReasonInHistory` | Failed; trimmed reason in history | Pass |
| TC17S-23 | BR15 | `Act_DeliverOrFailOnReadyOrderNotPickedUp_ThrowsBr15AndLeavesOrderUnchanged` | Cannot skip pickup | Pass |
| TC17S-24 | BR15 | `Act_AnyActionOnDeliveredOrFailedOrder_ThrowsBr15AndLeavesOrderUnchanged` | Terminal states final | Pass |
| TC17S-25 | NFR05 | `Act_StaleRowVersion_ThrowsConcurrencyAndLeavesOrderUnchanged` | Double submit rejected | Pass |
| TC17S-26 | FR17 | `Act_UnknownOrder_ThrowsNotFound` | `NotFound` | Pass |
| TC17S-27 | FR17, NFR06 | `FullDispatchFlow_AssignPickUpDeliver_ReachesDeliveredWithTwoHistoryRows` | Assign → pick up → deliver; 2 history rows | Pass |
| TC17S-28 | BR15, NFR04 | `Transition_KitchenStaffOrOtherRolesOnShipperTransitions_ThrowBr15AndLeaveOrderUnchanged` | Only shippers can do shipper transitions | Pass |

---

## 3.4.3 Manual Test Cases (browser, IIS Express)

Accounts from the README (`admin@`, `kitchen.d1@`, `shipper1@`, `local@saigonbite.vn`).

| Test ID | FR / NFR | Steps | Expected Result | Actual Result | Status |
| :--- | :--- | :--- | :--- | :--- | :---: |
| MT-01 | FR14, FR16, FR19, FR17 | Admin opens `/Hubs`, `/HubReports/HubUtilization`, `/Dispatch` | Pages load in VI and EN | Loaded (smoke test 27 Nov, Chromium) | Pass |
| MT-02 | FR14.1 | Admin creates a hub with capacity `0` | Error shown on the capacity field; nothing saved | Error shown (27 Nov) | Pass |
| MT-03 | FR17 | Customer places an order at the District 1 hub → kitchen Confirm → Start preparing → Ready → Admin assigns `shipper1` → shipper Pick up → Delivered (COD, cash ticked) | Each step succeeds and the customer's order page shows the new status | Not yet run end to end in the browser; the same flow passes as TC17K-6 + TC17S-27. **Run and screenshot before the demo** | To run |
| MT-04 | NFR03 | Repeat MT-01 at 375 px width | Layout readable, no horizontal scroll | Readable (27 Nov) | Pass |
| MT-05 | NFR04 | Not signed in, open `/Hubs`, `/Kitchen`, `/Shipper` | Redirect to `/Account/Login` | Redirected (27 Nov) | Pass |
| MT-06 | NFR04 | Signed in as kitchen or shipper, open `/Hubs` or `/Dispatch` | Access denied (HTTP 403, like UC02 admin pages) | Found by code review (28 Nov): **redirected to `/Account/Login` instead of 403.** Access is still blocked, but the response is inconsistent with the UC02 pages | Known issue |
| MT-07 | NFR04 | Admin page open, session expires, then click the rain-flag button | Message asking the user to sign in again | Found by code review (28 Nov): **no message; the button does nothing** (`uc03.js` reads `null` data after the redirect to the login page) | Known issue |

## Known gaps and follow-up
1. **MT-06:** in `RoleOnlyAttribute.HandleUnauthorizedRequest`, return 403 when the user is signed in and redirect to login only when they are not.
2. **MT-07:** in `uc03.js`, treat a response that is not JSON, or a redirect, as "session expired", and add a `.catch` for network errors.
3. There are no automated controller-level tests for the role filters. Authorization is tested at the service level (TC17K-4/5, TC17S-6/10/12/13/28) and manually (MT-05/06).

---

## 3.4.4 Execution Evidence

```text
vstest.console.exe SaigonBite.Tests.dll /Tests:HubServiceTests,RainSettingServiceTests,HubReportServiceTests,KitchenBoardServiceTests,ShipperServiceTests
  Passed Create_PersistsHubAndBrandLinksAtomicallyAndReturnsCreated [49 ms]
  ...
  Passed ListAndGet_LoadCountsOnlyConfirmedAndPreparing_AndClassifiesThresholds [67 ms]
  Passed ExistingOrderKeepsHeavyRainSnapshotAfterFlagChanges_WhileNewQuoteUsesNewFlag_TC16 [229 ms]
  Passed FullDispatchFlow_AssignPickUpDeliver_ReachesDeliveredWithTwoHistoryRows [50 ms]
Total tests: 75
     Passed: 75
 Total time: 7.8286 Seconds

vstest.console.exe SaigonBite.Tests.dll   (whole solution)
Test Run Successful.
Total tests: 170
     Passed: 170
 Total time: 7.0154 Seconds
```

> For the final PDF, add a screenshot of this run and screenshots of MT-01 to MT-04.
