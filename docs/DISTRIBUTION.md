# How the wrapper and Microsoft.GameInput work together

This document explains how **GameInputSharp.Core** relates to the official **Microsoft.GameInput** NuGet and why you are not packaging or redistributing Microsoft’s files.

---

## NuGet fetches the package, but the package has no runtime DLL

When you run **`dotnet restore`** or build the solution, NuGet **does** fetch **Microsoft.GameInput** from nuget.org. The package is stored in your **NuGet cache** (e.g. `%USERPROFILE%\.nuget\packages\microsoft.gameinput\3.2.138\`), not copied into your workspace. So you won’t see “official GameInput files” inside your Cursor project folder — that’s normal. The dependency is satisfied for build; the package is on disk in the cache.

The important part: the **Microsoft.GameInput NuGet package does not contain GameInput.dll or GameInputRedist.dll**. It contains only:

- C++ headers (`GameInput.h`, etc.)
- A C++ link library (`GameInput.lib`) and source for C++ projects

So even when the package is restored, **there is no runtime DLL for the wrapper to load from the package**. The wrapper is written to load the native DLL from:

1. The application directory (next to your exe)
2. **System32** (where the Windows / GameInput redist installs it)

If the DLL in System32 exists but **load fails** (e.g. Win32 error 126), that almost always means a **dependency of the DLL** is missing — typically the **Visual C++ Redistributable (x64)**. Installing it (e.g. [latest VC++ Redist](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)) fixes the load; the NuGet package does not need to “provide” the DLL because it never did.

**Summary:** The wrapper has “something to wrap” because it loads the **runtime DLL from the machine** (System32 or app directory), not from the NuGet package. Not seeing GameInput files in your workspace is expected; the package is in the cache and doesn’t contain a loadable DLL. Your load failure is from the System32 DLL’s missing dependency (VC++ Redist), not from a missing NuGet or missing files in the project.

---

## What the developer does

A developer who wants to use GameInput from C#:

1. Adds the **official Microsoft.GameInput** NuGet to their project (from nuget.org, published by Microsoft).
2. Adds **your** NuGet (**GameInputSharp.Core**) as a wrapper so they can call the API from C#.

They can add both explicitly, or they can add only **GameInputSharp.Core** and get **Microsoft.GameInput** automatically (see below).

---

## What “dependency” means (you are not packaging Microsoft’s files)

In **GameInputSharp.Core.csproj** you have:

```xml
<PackageReference Include="Microsoft.GameInput" Version="3.2.138" />
```

That is a **dependency declaration**, not a copy of Microsoft’s package.

- When you run **`dotnet pack`**, the built **GameInputSharp.Core** NuGet package (`.nupkg`) contains **only your content**: your DLL(s), README, and any docs you include. It does **not** include Microsoft.GameInput’s files.
- Inside your `.nupkg`, the package metadata says: “This package **depends on** Microsoft.GameInput (version 3.2.138).” So your package is just **metadata + your wrapper**, not Microsoft’s code or binaries.

So the “C++ dependency” is **not** inside your package. It is a **reference** that tells NuGet: “Whoever installs GameInputSharp.Core must also have Microsoft.GameInput available.”

---

## What happens when someone installs your NuGet

When a developer runs:

```bash
dotnet add package GameInputSharp.Core
```

NuGet will:

1. Download **GameInputSharp.Core** from the feed where you publish it (your package, your files only).
2. See that GameInputSharp.Core **depends on** Microsoft.GameInput.
3. Download **Microsoft.GameInput** from **nuget.org** (Microsoft’s package, Microsoft’s feed).
4. Restore both into the developer’s project (e.g. `packages/` and project references).

So:

- **You** never ship Microsoft’s package. You only declare that your package **requires** it.
- **Microsoft.GameInput** is always pulled from **nuget.org** by NuGet when your package is installed. The developer ends up with the official Microsoft package in their solution; you are not repackaging or redistributing it as your own.

That’s the normal NuGet pattern: your package has a **dependency**, and the package manager resolves it from the official source.

---

## Summary

| Concern | Reality |
|--------|--------|
| “The C++ dependency is in my project.” | Your **project** (and your **package**) declare a **dependency** on Microsoft.GameInput. Your repo and your .nupkg do **not** contain Microsoft’s source or binaries. |
| “I can’t package Microsoft files as my own.” | You don’t. Your package contains only the GameInputSharp wrapper. Microsoft.GameInput is fetched by NuGet from nuget.org when someone installs your package. |
| “The developer should add the official Microsoft NuGet.” | They can add it themselves, or they can add only **GameInputSharp.Core**; in the second case, NuGet will add **Microsoft.GameInput** as a transitive dependency from nuget.org. Either way, they get the official Microsoft package, not a repackaged copy from you. |

So: the dependency in **GameInputSharp.Core** is the correct and standard way to say “this wrapper requires the official Microsoft.GameInput package.” It does not bundle or repackage Microsoft’s files; it only ensures that when someone uses your wrapper, they get the official Microsoft NuGet from nuget.org.
