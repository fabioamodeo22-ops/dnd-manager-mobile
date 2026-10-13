using System;
using System.Collections.Generic;
using System.Linq;

namespace DnDManager.Services
{
    /// <summary>
    /// Genera spunti narrativi per sbloccare una sessione, ispirati alle classiche "tabelle da master".
    /// Funziona interamente offline: nessuna chiave API, nessuna connessione richiesta.
    /// </summary>
    public static class StoryPromptService
    {
        private static readonly Random Rnd = new();

        private static readonly Dictionary<string, string[]> Categories = new()
        {
            ["complicazione"] = new[]
            {
                "Qualcosa che i personaggi davano per scontato si rivela falso o incompleto.",
                "Un alleato fidato chiede un favore che mette in imbarazzo il gruppo.",
                "Le risorse che contavano di usare (denaro, contatti, equipaggiamento) risultano insufficienti o compromesse.",
                "Un rivale con lo stesso obiettivo li anticipa o li affianca inaspettatamente.",
                "Le conseguenze di un'azione passata del gruppo tornano a galla nel momento peggiore.",
                "Un testimone chiave scompare, muore o si rifiuta improvvisamente di parlare.",
            },
            ["colpo di scena"] = new[]
            {
                "Chi sembrava la vittima è in realtà complice (consapevole o meno) dell'antagonista.",
                "L'obiettivo che stavano cercando esiste già in loro possesso, nascosto in qualcosa che avevano ignorato.",
                "Il vero nemico non è chi credevano, ma qualcuno rimasto finora sullo sfondo.",
                "L'evento che ha dato il via alla missione è stato orchestrato per portare proprio il gruppo in quel luogo.",
                "Un dettaglio apparentemente innocuo, menzionato molte sessioni fa, si rivela la chiave di tutto.",
                "Due fazioni che sembravano nemiche stanno in realtà collaborando in segreto.",
            },
            ["nuovo alleato o nemico"] = new[]
            {
                "Un personaggio secondario già incontrato ricompare con motivazioni cambiate.",
                "Uno sconosciuto offre aiuto prezioso, ma a un prezzo che si scoprirà solo più avanti.",
                "Un membro della fazione avversaria diserta e si offre di collaborare col gruppo.",
                "Arriva un rinforzo inatteso, ma con un'agenda propria che non coincide del tutto con quella del gruppo.",
                "Una creatura o un mostro incontrato in precedenza torna, ma le circostanze sono cambiate.",
                "Un concorrente diretto compare per contendere ai personaggi lo stesso obiettivo.",
            },
            ["dilemma morale"] = new[]
            {
                "Per raggiungere l'obiettivo serve fare un compromesso etico che divide il gruppo.",
                "Salvare qualcuno significa lasciare che qualcun altro subisca le conseguenze.",
                "L'unica soluzione rapida disponibile è anche quella meno onorevole.",
                "Un nemico sconfitto chiede pietà, offrendo informazioni preziose in cambio della vita.",
                "Rivelare la verità danneggerebbe un innocente; nasconderla protegge il colpevole.",
                "Il gruppo scopre che un proprio alleato ha mentito per una causa che ritiene giusta.",
            },
            ["pressione del tempo"] = new[]
            {
                "Un evento imminente (un rituale, un'eclissi, un arrivo) impone una scadenza netta.",
                "Ogni ora di ritardo peggiora visibilmente la situazione di qualcuno che i personaggi vogliono aiutare.",
                "Un rivale sta avanzando più rapidamente verso lo stesso obiettivo.",
                "Le condizioni ambientali (meteo, marea, stagione) stanno per rendere il percorso impraticabile.",
                "Qualcuno di importante minaccia di agire autonomamente se il gruppo non si muove subito.",
                "Le voci sulla missione del gruppo iniziano a circolare, attirando attenzioni indesiderate.",
            },
            ["rivelazione"] = new[]
            {
                "Un PNG chiave rivela un legame di sangue o di storia con uno dei personaggi.",
                "Un luogo che i personaggi hanno già visitato nasconde una seconda funzione o un livello inferiore.",
                "Un oggetto che portano con sé da tempo ha un'origine o un potere che non conoscevano.",
                "Un evento storico raccontato in modo distorto viene finalmente chiarito, cambiando le prospettive.",
                "La vera identità di un antagonista o alleato viene smascherata.",
                "Ciò che sembrava un caso isolato fa parte di uno schema molto più ampio.",
            },
            ["ambiente e pericolo"] = new[]
            {
                "Un pericolo ambientale (crollo, inondazione, incendio) costringe a un cambio di piano improvviso.",
                "Il luogo in cui si trovano non è disabitato come pensavano: qualcosa lo sorveglia.",
                "Un'imboscata li coglie mentre sono più vulnerabili (feriti, senza risorse, divisi).",
                "Le vie di fuga o di ritorno sono state bloccate o rese impraticabili.",
                "Una creatura territoriale reagisce all'intrusione del gruppo in modi imprevisti.",
                "Il terreno stesso lavora contro di loro: magia residua, instabilità, illusioni.",
            },
            ["aggancio per la prossima sessione"] = new[]
            {
                "Un messaggero raggiunge il gruppo con notizie urgenti da un luogo che non hanno ancora esplorato.",
                "Uno dei personaggi riceve un sogno, visione o presagio che lascia più domande che risposte.",
                "Un evento in una città lontana inizia a proiettare conseguenze verso il gruppo.",
                "Una taglia, una lettera o un contratto lega il gruppo a un nuovo impegno.",
                "Un simbolo o segno ricorrente inizia a comparire in luoghi inaspettati.",
                "Qualcuno che il gruppo credeva morto o scomparso viene avvistato.",
            },
        };

        // parole chiave nella richiesta del master -> categoria più pertinente
        private static readonly (string[] Keywords, string Category)[] KeywordHints =
        {
            (new[] { "blocc", "fermo", "stallo", "non sanno" }, "aggancio per la prossima sessione"),
            (new[] { "nemic", "antagonist", "cattivo", "villain" }, "nuovo alleato o nemico"),
            (new[] { "tempo", "scadenza", "fretta", "urgen" }, "pressione del tempo"),
            (new[] { "morale", "scelta", "dilemma", "difficile" }, "dilemma morale"),
            (new[] { "colpo di scena", "twist", "sorpresa" }, "colpo di scena"),
            (new[] { "ambiente", "pericolo", "trappola", "luogo" }, "ambiente e pericolo"),
            (new[] { "verit", "segreto", "riveler", "scopr" }, "rivelazione"),
            (new[] { "complica", "problema", "ostacolo" }, "complicazione"),
        };

        /// <summary>Restituisce 2 spunti narrativi, scelti in base alla richiesta (se pertinente) o casualmente.</summary>
        public static string Generate(string? request, string campaignName)
        {
            var picked = new List<(string Category, string Prompt)>();

            var category = MatchCategory(request);
            if (category != null)
                picked.Add((category, PickFrom(category)));

            // completa fino a 2 suggerimenti da categorie diverse, scelte a caso
            var remaining = Categories.Keys.Where(k => picked.All(p => p.Category != k)).OrderBy(_ => Rnd.Next()).ToList();
            while (picked.Count < 2 && remaining.Count > 0)
            {
                var cat = remaining[0];
                remaining.RemoveAt(0);
                picked.Add((cat, PickFrom(cat)));
            }

            var lines = picked.Select((p, i) => $"{i + 1}. [{Capitalize(p.Category)}] {p.Prompt}");
            var header = string.IsNullOrWhiteSpace(campaignName)
                ? "Un paio di spunti per rilanciare la sessione:"
                : $"Un paio di spunti per rilanciare \"{campaignName}\":";
            return header + "\n\n" + string.Join("\n\n", lines);
        }

        private static string? MatchCategory(string? request)
        {
            if (string.IsNullOrWhiteSpace(request)) return null;
            var lower = request.ToLowerInvariant();
            foreach (var (keywords, category) in KeywordHints)
                if (keywords.Any(k => lower.Contains(k)))
                    return category;
            return null;
        }

        private static string PickFrom(string category) => Categories[category][Rnd.Next(Categories[category].Length)];

        private static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
    }
}
