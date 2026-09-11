# Matricea de acoperire JavaScript

Aceasta matrice leaga responsabilitatile observabile ale celor sase fisiere JavaScript initiale de testele automate din `tests/`. Testele folosesc fisierele reale de productie si HTML-ul real al extensiei.

Modificarea existenta a utilizatorului care seteaza popup-ul la `468px` este tratata drept contractul vizual curent acceptat. Testul static o protejeaza fara sa modifice `popup.html`.

| Fisier initial | Responsabilitati observabile | Teste si scenarii | Limite fara Chrome real |
|---|---|---|---|
| `extractors/jsonLdImporter.js` | Descopera reteta in JSON-LD; accepta `@type` string sau array; traverseaza radacina, array-uri, obiecte si `@graph`; normalizeaza autorul, imaginea, ingredientele si instructiunile; foloseste URL-ul paginii. | `jsonLdImporter.test.js`: lipsa scripturilor, JSON invalid urmat de JSON valid, toate formele de imbricare, variantele autorului si imaginii, filtrare si `trim()`, instructiuni string/array/`HowToStep`/structuri imbricate, `sourceUrl`. | Parsarea si DOM-ul sunt reproduse de `jsdom`; nu este verificata injectarea efectiva prin `chrome.scripting`. |
| `extractors/htmlImporter.js` | Citeste familiile de selectori structurati; curata spatii si duplicate; foloseste fallback-urile de articol; recunoaste titluri romanesti si englezesti; extrage autorul si imaginea in ordinea de prioritate; valideaza campurile obligatorii. | `htmlImporter.test.js`: toate familiile de selectori existente, fixture-uri structurate si fallback, liste si paragrafe, titluri RO/EN, autor meta si vizibil, prioritatea OG/Twitter/itemprop/articol, rezultat `null`, URL-ul documentului. | `jsdom` reproduce selectarea DOM si rezolvarea URL-urilor, dar nu stilurile sau markup-ul alterat dinamic de pagini reale. |
| `extractors/recipeExtractor.js` | Alege JSON-LD inaintea HTML; nu apeleaza fallback-ul dupa un succes; raporteaza metoda; respinge rezultate nule sau primitive; pastreaza mesajul de esec. | `recipeExtractor.test.js`: prioritate JSON-LD, fallback HTML, contoare de apel, metodele `json-ld`/`html`, rezultate invalide si mesajul exact de esec. | Integrarea cu tab-ul activ este acoperita static in suita popup; executia reala in isolated world ramane verificare manuala. |
| `popup.js` | Compune controllerul; citeste tab-ul activ; injecteaza extractoarele in ordinea stabilita; raporteaza erorile API-urilor Chrome; salveaza `recipeToPrint`; deschide `edit.html` si `print.html` in ferestre popup `900x700`; ramane bootstrap clasic cu import dinamic. | `popupController.test.js`: flux comportamental cu mock pentru selectarea tab-ului activ, cele doua apeluri `executeScript`, `tabId`, ordinea fisierelor, rezultatul extragerii si respingerea extragerii; verificari statice ale `popup.html` si `popup.js` pentru importul dinamic, compunere, raportarea erorilor, cheia storage, URL-uri, tip si dimensiuni; confirma si latimea curenta acceptata de `468px`. | Nu se deschide Chrome si nu se injecteaza intr-un tab real; bootstrap-ul asincron ramane verificat static, iar secventa API Chrome extrasa este verificata comportamental. |
| `edit.js` | Citeste si salveaza `recipeToPrint`; compune controllerul cu navigare, inchidere si raportare erori; este entrypoint ES module. | `editController.test.js`: verificari statice ale HTML-ului si entrypoint-ului; controllerul este testat separat pentru payload, populare, randuri, focus, butoane, erori, submit, salvare, navigare, anulare si erori de callback. `editLogic.test.js` acopera URL-uri, normalizare, validare, pastrarea proprietatilor si imutabilitate. | `localStorage`, navigarea si inchiderea sunt mock-uri locale; restrictiile reale ale paginii de extensie raman verificare manuala. |
| `print.js` | Citeste `recipeToPrint`; compune controllerul cu `window.print()` si timerele reale; este entrypoint ES module. | `printController.test.js`: verificari statice ale HTML-ului si entrypoint-ului; payload absent, JSON corupt, primitive, array si structuri fara listele obligatorii; randare completa si defensiva, text sigur, imagine esuata, fara imagini, `load`, `error`, timeout controlat de 10 secunde, intarziere de 200 ms si o singura printare. | Dialogul real de printare si incarcarea efectiva a imaginilor nu sunt pornite; efectele si timerele sunt simulate. |

## Modulele extrase prin planul 13

| Modul | Acoperire |
|---|---|
| `editLogic.js` | `editLogic.test.js` verifica toate functiile publice si contractele de validare/normalizare. |
| `controllers/editController.js` | `editController.test.js` foloseste `edit.html` real si dependente injectate. |
| `controllers/popupController.js` | `popupController.test.js` foloseste `popup.html` real si dependente injectate, inclusiv raportarea unei extrageri respinse si revenirea la starea de esec. |
| `controllers/printController.js` | `printController.test.js` foloseste `print.html` real, efecte mock si timere controlate. |

## Verificare manuala ramasa

Suita automata nu inlocuieste reincarcarea extensiei unpacked si verificarea fluxurilor reale: injectarea in tab, storage-ul extensiei, ferestrele popup, navigarea, imaginile si dialogul de printare.
