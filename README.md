# Room Decor

A phone app for planning a room before you buy anything. Pick the room type,
enter its size, place furniture in 3D, walk around it, and see what it would cost.

This is the **phase 1 prototype** for iPhone: typed-in room sizes, generic 3D
furniture, room-type recommendations and a cost estimate, all running on
made-up sample products. Room scanning (Apple RoomPlan), real retailer
catalogs and Android come in later phases.

## What works now

1. **Choose a room**: bedroom, living room, dining room, home office or kids' room.
2. **Enter the size**: width, length and ceiling height, shown in meters and feet.
3. **Get suggestions**: essentials first (bedroom: bed frame, mattress; living
   room: sofa, coffee table, rug), then nice-to-haves. Only items that fit the room are shown.
4. **Place furniture**: tap an item to select it, drag it across the floor,
   rotate it in quarter turns. Items that overlap or leave the room turn red. Rugs may sit under furniture.
5. **Look around**: 3D orbit (drag to turn, pinch to zoom), Plan (top-down), and Walk (eye height, with forward and back buttons).
6. **See the cost**: a total with each item's store and price-check date,
   grouped repeats (four chairs), totals by store, and the price disclaimer.

## Open it

You need a Mac with [Unity Hub](https://unity.com/download), Unity **6000.0 LTS**
with the **iOS Build Support** module, and Xcode.

1. In Unity Hub choose **Add → Add project from disk** and pick this folder.
   If Hub offers a newer 6000.0 version, that's fine.
2. When the project opens, it creates `Assets/Scenes/Main.unity` and adds it to
   Build Settings. If it doesn't, use the menu **Room Decor → Set Up Main Scene**.
3. Press **Play**. In the editor the mouse stands in for touch; the scroll wheel zooms.

The touch code uses Unity's classic Input Manager. If Play shows an
`InvalidOperationException` about input, open **Edit → Project Settings →
Player → Other Settings → Active Input Handling** and choose **Both**.

### Run it on an iPhone

**File → Build Profiles** (or Build Settings) → **iOS** → **Switch Platform** →
**Build**, then open the generated Xcode project, choose your Apple ID team
under Signing & Capabilities, plug in the phone and press Run.

## How the code is laid out

| Folder | What it holds |
| --- | --- |
| `Assets/Scripts/Core` | Plain C# with no Unity dependency: rooms, catalog items, recommendation rules, placement checks, cost estimate. Fully tested. |
| `Assets/Scripts/App` | The Unity side: screens, touch input, cameras, and the generic 3D shapes. |
| `Assets/Scripts/Editor` | Creates the Main scene on first open. |
| `Assets/Resources/sample_catalog.json` | Made-up products for testing. |
| `Tests/Core.Tests` | Tests for `Core`, run with `dotnet test Tests/Core.Tests` and on every pull request. |

## Rules this project keeps (to stay safe for the App Store)

- **Products come only from official sources**: retailers' affiliate feeds or
  official APIs, synced by our own server. Never scrape retailer websites.
- **Our own 3D shapes**: furniture is drawn as generic shapes sized to the
  product's real dimensions and painted in its color from the feed. Never copy
  a retailer's 3D or AR model files.
- **Official photos, shown as supplied**: product photos come only from the
  feed's image links and appear in the suggestions and cost lists. Don't crop,
  cut out or edit them, and don't place them into the 3D room, unless a brand
  gives written permission.
- **No retailer logos**: show store names as plain text unless a program's brand rules allow more.
- **Prices are estimates**: show when each price was checked and the
  disclaimer in `CostEstimate.Disclaimer`. Refresh prices at least daily.
- **Say we earn commissions** wherever there's a Buy link.
- **No secrets in this repository**: affiliate and API keys live on the server,
  never in the app or in git. This repository is public.

## Next phases

1. **Real catalog**: join affiliate programs, sync feeds nightly on a small
   server, replace `sample_catalog.json` with the server's catalog.
2. **Scanning and AR**: RoomPlan on LiDAR iPhones, AR view of the furniture in the real room.
3. **Store launch**: privacy policy, terms, in-app account deletion, TestFlight beta, App Store review.
