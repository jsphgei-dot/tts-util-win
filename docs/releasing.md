[Documentation index](index.md) | [Usage](usage.md) | [Build](building.md) | [Release](releasing.md) | [Develop](developing.md)

---

# 📦 Release

Cutting a release means building the artifacts, hashing them, attaching them to a GitHub
release and publishing the manifest the update check reads. Version numbers come from
`Directory.Build.props` and nowhere else, so a release starts by bumping the two values there.

## Publishing a build

```powershell
# Build, zip, hash, and write dist\latest.json without publishing anything.
.\scripts\Publish.ps1

# The same, then create the release in the public distribution repository and
# push the manifest that makes the update notice appear.
.\scripts\Publish.ps1 -Publish -NotesFile dist\RELEASE_NOTES_v0.3.0-beta.md
```

The manifest is pushed **after** the assets, so nobody is pointed at a download that is not
there yet. The seed files for that public repository, its README and the changelog, live in
`distribution\` here.

## Where a build looks for its update

Every build reads a `latest.json` URL that was compiled into it, so a copy already installed
keeps asking the address it shipped with, whatever the current one is. Changing that address is
therefore an overlap rather than a switch: for as long as older copies are worth supporting,
each version is published to both repositories, with the same tag, the same assets, the same
hashes, and a manifest in each pointing at its own copy of its own assets.

## The changelog

`distribution\CHANGELOG.md` is an embedded resource, so the Updates tab lists it without
fetching anything, and every `## ` heading in it becomes a release row there. Changes land under
the `Unreleased` heading at the top as they are made, and the release step renames that heading
to the version and the date. A release whose changelog entry is still called `Unreleased` ships
a program that says so.

## Code signing

The build is unsigned by default, which is why SmartScreen warns the first time a setup runs,
and why it sometimes blocks the setup outright with no way through the dialog. The public README
tells readers how to turn **Check apps and files** off in Windows Security when that happens.
Both scripts take a certificate thumbprint and sign with it when one is given:

```powershell
.\scripts\Publish.ps1 -Publish -NotesFile dist\notes.md -CertThumbprint ABC123DEF456
```

The certificate is looked up in the current user's store by thumbprint, so nothing secret is
written down here. `TtsUtilWin.exe` is signed before the installer is compiled, so the
installed copy carries the signature too, and the setup is signed after Inno Setup builds it.
Both signatures are timestamped, which keeps them valid after the certificate expires, and
both are verified with `signtool verify /pa` before the build is allowed to finish. Leave
`-CertThumbprint` off and the build is unsigned exactly as before.
