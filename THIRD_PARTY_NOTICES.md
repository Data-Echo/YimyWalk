# Third-party notices

## Adrianotiger/desktopPet (eSheep)

- Source: <https://github.com/Adrianotiger/desktopPet>
- Snapshot: `e2933305157a34818f47d238c74607593d81816d`
- Fetched: 2026-10-04
- License stated by upstream README: MIT License.
- Snapshot location: `vendor/esheep/`.

The upstream repository contains code, NuGet references, and many pet images/audio assets. The MIT notice applies to the upstream project as stated; individual bundled assets may have separate attribution or provenance. YimyWalk does not assume every bundled pet is redistributable. Any asset copied into a release must be audited separately and retain its attribution.

The upstream README credits NAudio and several sprite authors. We keep the snapshot for audit and do not yet include its character media in a YimyWalk release.

## SixLabors.ImageSharp 3.1.12

- Package: `SixLabors.ImageSharp` 3.1.12
- Source: <https://github.com/SixLabors/ImageSharp>
- License: Six Labors Split License 1.0 (Apache 2.0 for qualifying open-source/source-available, qualifying lower-revenue or nonprofit use; other direct commercial use may require the Six Labors commercial license).
- Use in YimyWalk: decode `sprite.png` into platform-neutral RGBA frame buffers in `YimyWalk.Core`.
- The complete license text shipped with the source and release package is in `licenses/ImageSharp-LICENSE.txt`; the Apache 2.0 text referenced by the split license is in `licenses/Apache-2.0.txt`.

## YimyWalk sample roles

- `Characters/cat/sprite.png` and `Characters/slime/sprite.png` are the YimyWalk sample role assets included in this repository.
- They are not copied from the `vendor/esheep/` snapshot. The upstream snapshot's bundled media is not included in the release.
