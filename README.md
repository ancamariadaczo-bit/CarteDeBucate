# CarteDeBucate

[![Tests](https://github.com/ancamariadaczo-bit/CarteDeBucate/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/ancamariadaczo-bit/CarteDeBucate/actions/workflows/tests.yml)

A personal recipe management application built with .NET.

The project was created both as a practical way to organize recipes and as an opportunity to refresh and expand my .NET development skills.

## About the project

I had recipes saved in many different places: browser bookmarks, notes, screenshots, photos and other files. Finding a particular recipe again was often harder than it needed to be.

CarteDeBucate brings those recipes together in one personal library where they can be added, imported, searched and updated.

The project can be used through a web interface or a console application. It also includes Recipe Clipper, a companion Chrome extension that extracts a recipe from the current web page so it can be reviewed, edited and printed or saved as a PDF directly from the browser.

## Current functionality

The project currently supports:

* adding recipes manually or importing them from a web address
* viewing, editing and deleting saved recipes
* storing ingredients, preparation steps, notes and the original source link
* organizing recipes by status, such as **To try**, **Tried** or **Favorite**
* searching the recipe library and browsing longer result lists page by page
* exporting recipes to a JSON backup and importing them again later
* keeping each registered user's recipes separate when authentication is enabled
* choosing between a web interface and two console interface styles
* storing application data in SQLite, with JSON storage also available to the console application
* extracting, reviewing and printing recipes with the Recipe Clipper Chrome extension
* automated C# and JavaScript tests, continuous integration with GitHub Actions and deployment through Render

## Project structure

```text
CarteDeBucate.Core
    Shared recipe models, storage, importing and application logic

CarteDeBucate.App
    Console application with classic and rich interface modes

CarteDeBucate.Web
    ASP.NET Core MVC web application

CarteDeBucate.Tests
    Automated tests for the .NET applications

CarteDeBucate.ChromeExtension
    Recipe Clipper extension and its JavaScript tests
```

## Technologies

The project uses:

* C#
* .NET 10
* ASP.NET Core MVC
* SQLite
* JSON
* Spectre.Console
* HtmlAgilityPack
* HTML / CSS / JavaScript
* Chrome Extension Manifest V3
* xUnit
* Node.js test runner and jsdom
* Git
* GitHub Actions
* Render

## Testing

The project is checked locally before a push and again on GitHub after the code is uploaded.

### Before push

A local Git `pre-push` hook runs the .NET tests:

```bash
dotnet test
```

It also runs the Recipe Clipper JavaScript tests:

```bash
npm test
```

If any test fails, the push is cancelled.

### GitHub Actions

After a successful push, GitHub Actions builds the .NET projects and runs the C# and JavaScript tests again in a clean environment.

Render is configured to deploy only after the CI checks pass.

For more information, see [Testing and Deployment](documentation/TestingAndDeployment.md).

## Development workflow

Typical workflow:

```bash
git add .
git commit -m "Description of changes"
git push
```

The push automatically triggers the local tests.

If any local test fails, the push is cancelled and the code is not sent to GitHub.

After the code reaches GitHub:

```text
GitHub Actions
      ↓
C# tests + JavaScript tests
      ↓
Tests pass
      ↓
Render deployment
```

## Planned features

Ideas for future versions include:

* locally stored recipe images, personal result photos and attachments
* backups that also include locally stored photos
* ingredient and measurement conversion
* recipe ratings and labels
* cooking history
* nutrition calculations
* ingredient substitutions
* diet-specific suggestions
* advanced search
* shopping list generation
* meal planning

## Documentation

The documentation is split into two directories with different purposes:

* [`docs`](docs/) contains the Recipe Clipper privacy policy published through GitHub Pages
* [`documentation`](documentation/) contains internal project documentation and development notes

Current documentation includes:

* [Testing and Deployment](documentation/TestingAndDeployment.md)
* [Recipe Clipper release checklist](documentation/RecipeClipperRelease.md)
* [Recipe Clipper privacy policy](docs/index.html)

## Status

The project is under active development.
