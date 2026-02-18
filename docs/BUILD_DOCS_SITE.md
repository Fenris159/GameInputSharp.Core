# Building the offline documentation site

The GameInputSharp.Core repository includes an **MkDocs** site that becomes the offline “Wikipedia” for the wrapper. It is built separately and then included in the NuGet package when you run `dotnet pack`.

---

## Prerequisites

- **Python 3** (e.g. from [python.org](https://www.python.org/) or `winget install Python.Python.3.12`)
- **pip** (usually with Python)

---

## Build the site

From the **repository root** (where `mkdocs.yml` lives):

```bash
pip install -r requirements-docs.txt
mkdocs build
```

Output is in the **`site/`** directory (static HTML, CSS, JS). Open `site/index.html` in a browser to view it locally.

Optional: serve with live reload while editing:

```bash
mkdocs serve
```

Then open http://127.0.0.1:8000 .

---

## Include the site in the NuGet package

1. Build the site (see above) so that the `site/` folder exists at the repo root.
2. From the repo root or the library project directory, run:

   ```bash
   dotnet pack src/GameInputSharp.Core/GameInputSharp.Core.csproj -c Release
   ```

The built package will contain a **`docs-site/`** folder with the full static site. Consumers can extract the `.nupkg` (or open the package folder after install) and open **`docs-site/index.html`** in a browser for offline documentation.

If you run `dotnet pack` **without** having run `mkdocs build`, the pack still succeeds; the package just won’t contain the `docs-site/` folder. The project also runs `mkdocs build` automatically before pack when `mkdocs.yml` exists (if `mkdocs` is on the PATH); otherwise that step is skipped without failing the build.

---

## Contents of the site

- **Home** — Overview, quick start, documentation map
- **Quick start** — Get running in minutes
- **Full usage guide** — Installation, polling, haptics, callbacks, disposal
- **Code examples** — Copy-paste snippets
- **API & mapping** — Constants, flags, axis/button mapping
- **Guides** — Compatibility, distribution, security
- **Reference** — API alignment, changelog, audit, test results
- **Troubleshooting** — Common issues and FAQ
- **Glossary** — Term definitions

All content is in the **`docs/`** folder; `mkdocs.yml` defines the navigation and theme (Material).
