# Archived Projects

## CarteDeBucate.App

**Status:** Archived — No longer maintained.

**Archive date:** November 19, 2026

**Last working version:** `console-v1-archive`

This project was the original console application for CarteDeBucate.

It has been removed from the active .NET solution because future development will focus on the Web application and Chrome Extension.

The console application will not be migrated to the new email-based authentication system.

### Restoring the original version

To inspect the last working version, run:

```bash
git switch --detach console-v1-archive
```

To return to the active development branch:

```bash
git switch main
```

The archived project is preserved for historical reference and is not guaranteed to build against future versions of the shared libraries.