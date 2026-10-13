# Grimorio del Master: app mobile (Android e iOS)

Versione per telefono e tablet del **D&D Campaign Manager**. Usa **lo stesso progetto Supabase** dell'app WPF per PC: account, campagne, personaggi, missioni, battaglie e file sono condivisi in tempo reale tra PC e telefono.

## Struttura

```
DnDManager.Mobile.sln
├─ src/DnDManager.Core      libreria condivisa (net8.0 + net10.0): accesso Supabase, modelli, regole D&D
├─ src/DnDManager.Mobile    app .NET MAUI Blazor Hybrid (net10.0-android, net10.0-ios)
├─ supabase/                script SQL da eseguire una volta su Supabase
├─ docs/privacy-policy.md   informativa privacy (gli store chiedono un URL pubblico)
└─ .github/workflows/       build cloud Android e iOS (GitHub Actions)
```

`DnDManager.Core` contiene i servizi `Remote*`, `SupabaseRest`, `SupabaseStorage`, i modelli e i dadi della versione WPF, con gli **stessi namespace**. Differenza principale: l'autenticazione (`SupabaseService`) non usa più l'SDK Supabase ma chiamate HTTP dirette a GoTrue. In più c'è il rinnovo automatico del token, così l'utente resta collegato tra un avvio e l'altro. Visto che il target è anche `net8.0`, in futuro la app WPF potrà referenziare questa libreria al posto delle sue copie dei servizi.

## Funzioni incluse (prima versione)

| Sezione | Master | Giocatore |
|---|---|---|
| Login / registrazione (email o nome utente) | ✓ | ✓ |
| Scelta e creazione campagne | ✓ | ✓ |
| Dashboard campagna (compagnia con PF, conteggio missioni) | modifica | lettura |
| Personaggi: scheda 5e completa, PF rapidi, ritratto, inventario | tutti, PG e NPC | solo i propri |
| Missioni | gestione completa, note riservate, blocco missioni collegate | solo "in corso" |
| Storia + spunti narrativi offline | ✓ | — |
| Appunti personali privati | ✓ | ✓ |
| Incantesimi, trucchetti, equipaggiamento | modifica + assegnazione | consultazione, filtro "solo i miei" |
| File della campagna (immagini e audio in-app, PDF esterni) | tutti | i propri |
| Manuali | ✓ | ✓ |
| Dadi (vantaggio/svantaggio, espressioni tipo 2d6+3) | ✓ | ✓ |
| Battaglia: griglia, mappa, token, iniziativa, sincronizzazione ogni 2 s | nebbia, muri, turni, PF | muove i propri token |
| Eliminazione account | ✓ | ✓ |

Non incluse per ora: generatore procedurale di mappe, dati di esempio, personalizzazione colori, opzioni di visibilità delle sezioni (nella versione PC sono salvate localmente sul PC e non su Supabase).

## Passo obbligatorio su Supabase

Apple e Google richiedono che un'app con registrazione permetta di **eliminare l'account dall'app**. Esegui una volta `supabase/delete_my_account.sql` nel SQL Editor di Supabase. Controlla le `delete` nel file: se le foreign key verso `profiles` sono già `on delete cascade`, alcune righe sono superflue ma innocue.

## Compilare in locale

Servono Visual Studio 2026 con il carico di lavoro **.NET MAUI** e .NET 10 SDK.

- **Android:** apri la soluzione, scegli un emulatore o un telefono collegato, premi F5.
- **iOS:** richiede un Mac. Senza Mac usa la pipeline cloud qui sotto.

## Build cloud (GitHub Actions)

### Android: parte a ogni push su `main`
Produce un **.aab** (per Play Store) e un **.apk** (da installare direttamente) negli *Artifacts* della run.

Per caricare su Play serve la firma con la tua chiave di upload. Crea il keystore **una volta** e conservalo (se lo perdi non potrai più aggiornare l'app):

```bash
keytool -genkeypair -v -keystore upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
base64 -w0 upload.keystore     # su Windows: [Convert]::ToBase64String([IO.File]::ReadAllBytes("upload.keystore"))
```

Secrets del repository (Settings → Secrets and variables → Actions):

| Secret | Valore |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | output del comando base64 |
| `ANDROID_KEYSTORE_PASSWORD` | password del keystore |
| `ANDROID_KEY_ALIAS` | `upload` |
| `ANDROID_KEY_PASSWORD` | password della chiave |

### iOS: si avvia a mano (Actions → iOS → Run workflow)
- Con `upload` **disattivato** fa solo una build di verifica per simulatore, senza bisogno di certificati.
- Con `upload` **attivo** firma l'app e la carica su **TestFlight**.

I minuti macOS nei repository privati valgono 10 volte quelli Linux. Per questo la pipeline iOS non parte da sola.

> Nota Xcode: i runner GitHub hanno al massimo Xcode 26.x, mentre l'ultimo workload .NET per iOS chiede Xcode 27. Per questo `ios.yml` fissa il workload iOS alla versione per SDK 26.5 (file di rollback) e usa Xcode 26.5. Quando i runner avranno Xcode 27 basta tornare a `dotnet workload install maui-ios`.
>
> Se una build fallisce, il log completo viene salvato nel branch `ci-logs`.

Preparazione su Apple (una volta):
1. [developer.apple.com](https://developer.apple.com) → Identifiers → registra l'App ID `com.fabioamodeo.dndmanager`. Se lo cambi, aggiorna `ApplicationId` nel `.csproj` e `bundle-id` in `ios.yml`.
2. Certificates → crea un certificato **Apple Distribution**. Senza Mac: genera la CSR con OpenSSL, poi scarica il `.cer` e convertilo in `.p12`:
   ```bash
   openssl req -new -newkey rsa:2048 -nodes -keyout dist.key -out dist.csr -subj "/emailAddress=fabio.amodeo.22@gmail.com/CN=Fabio Amodeo/C=IT"
   # carica dist.csr su Apple, scarica distribution.cer
   openssl x509 -inform DER -in distribution.cer -out dist.pem
   openssl pkcs12 -export -legacy -inkey dist.key -in dist.pem -out dist.p12
   ```
3. Profiles → crea un profilo **App Store Connect** per l'App ID. La pipeline lo scarica da sola.
4. [App Store Connect](https://appstoreconnect.apple.com) → Users and Access → Integrations → **App Store Connect API** → genera una chiave con ruolo *App Manager* e scarica il `.p8`.
5. App Store Connect → My Apps → **+** → crea l'app con lo stesso bundle ID.

| Secret | Valore |
|---|---|
| `IOS_P12_BASE64` | `base64` del file `dist.p12` |
| `IOS_P12_PASSWORD` | password scelta per il `.p12` |
| `APPSTORE_ISSUER_ID` | Issuer ID (pagina delle chiavi API) |
| `APPSTORE_KEY_ID` | Key ID della chiave |
| `APPSTORE_PRIVATE_KEY` | contenuto del file `.p8` (testo completo) |

## Quali file caricare negli store

| Store | File | Dove lo trovi |
|---|---|---|
| Google Play | `com.fabioamodeo.dndmanager-Signed.aab` | Actions → Android → run verde → Artifacts → `android-N` (zip). La firma è valida solo se i secrets `ANDROID_*` sono impostati; altrimenti la run mostra l'avviso "Nessun keystore". |
| Google Play | icona 512×512, immagine in evidenza 1024×500, screenshot | `play-icon-512.png`, `play-feature-1024x500.png`; gli screenshot li fai dal telefono |
| App Store | nessun file a mano | Actions → iOS → Run workflow con `upload` ✓: la build arriva su App Store Connect → TestFlight, poi la selezioni nella versione |
| App Store | screenshot iPhone 6,9" (1320×2868) e iPad 13" (2064×2752) | dal telefono/tablet, tramite TestFlight |
| Entrambi | testi, URL privacy e supporto | `docs/store-listing.md`; le pagine `docs/` sono pubblicate con GitHub Pages |

## Pubblicazione negli store: checklist

- **Versione:** `ApplicationDisplayVersion` nel `.csproj` è la versione visibile. Il numero di build viene preso in automatico dal numero della run di GitHub.
- **Privacy policy:** pubblica `docs/privacy-policy.md` a un URL pubblico (GitHub Pages, Notion, Google Sites…). Serve sia a Play sia ad Apple.
- **Account demo per la review:** Apple e Google devono poter entrare nell'app. Crea un utente di prova con una campagna d'esempio e indica le credenziali nelle note per la revisione.
- **Play Console:** prima il canale *Test interno*. I nuovi account sviluppatore personali devono fare un *test chiuso* (12 tester per 14 giorni) prima della produzione. Compila anche "Sicurezza dei dati" (email, ID utente, foto/file: raccolti, non condivisi, eliminabili).
- **App Store:** compila "Privacy dell'app" con gli stessi dati. Il manifest `PrivacyInfo.xcprivacy` è già incluso.
- **Contenuti di D&D:** usa solo materiale SRD 5.1 (CC-BY-4.0) e nessun logo o marchio "Dungeons & Dragons" nelle schermate dello store.
