# Bloom modules

Bloom is Sprig's add-on layer. A module is installed by placing one directory directly under `Bloom/`.

Each installed module must contain `bloom.ini`:

```ini
[bloom]
type = core
```

or:

```ini
[bloom]
type = pnp
```

The module id is its directory name. Keep ids to letters, numbers, `_`, `-`, and `.`.

## Layout

A PnP module can look like:

```text
Bloom/
  BundleForcer/
    bloom.ini
    main.cpp
    config.ini
    inc/
    src/
    bundles/
```

Core modules use the same `inc/` and `src/` conventions, but do not need `main.cpp` or `config.ini`.

Directories beginning with `_` or `.` are ignored by discovery. The templates in `_templates/` are therefore safe to keep in the repository.

See [docs/bloom.md](../docs/bloom.md) for the API and behavior.
