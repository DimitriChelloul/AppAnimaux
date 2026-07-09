param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$OutputMarkdown = (Join-Path $PSScriptRoot "AppAnimaux_Cours_Code_Ligne_Par_Ligne.md"),
    [string]$OutputPdf = (Join-Path $PSScriptRoot "AppAnimaux_Cours_Code_Ligne_Par_Ligne.pdf")
)

$ErrorActionPreference = "Stop"

$codeExtensions = @(
    ".cs", ".csproj", ".json", ".http", ".sql", ".yml", ".yaml", ".ps1", ".slnx"
)

$excludedPathParts = @(
    "\.git\", "\bin\", "\obj\", "\docs\generated\"
)

function Get-LanguageName {
    param([string]$Path)
    switch ([System.IO.Path]::GetExtension($Path).ToLowerInvariant()) {
        ".cs" { "csharp"; break }
        ".csproj" { "xml"; break }
        ".json" { "json"; break }
        ".http" { "http"; break }
        ".sql" { "sql"; break }
        ".yml" { "yaml"; break }
        ".yaml" { "yaml"; break }
        ".ps1" { "powershell"; break }
        ".slnx" { "xml"; break }
        default { "text"; break }
    }
}

function Escape-InlineCode {
    param([string]$Text)
    if ([string]::IsNullOrEmpty($Text)) {
        return "[ligne vide]"
    }

    return $Text.Replace('`', "'")
}

function Get-FileRole {
    param([string]$RelativePath)

    $normalized = $RelativePath.Replace("\", "/")
    if ($normalized -like "*.Api/Program.cs") {
        return "Point d'entree de l'API : il configure le serveur web, les dependances et les routes HTTP."
    }
    if ($normalized -like "*/Controllers/*.cs") {
        return "Controleur HTTP : il recoit les requetes de l'application et renvoie des reponses."
    }
    if ($normalized -like "*/Services/*.cs") {
        return "Service applicatif : il contient la logique metier principale."
    }
    if ($normalized -like "*/Repositories/*.cs") {
        return "Repository : il dialogue avec la base de donnees et isole le reste du code du SQL."
    }
    if ($normalized -like "*/Entities/*.cs") {
        return "Entite de domaine : elle decrit un objet important de l'application et ses donnees."
    }
    if ($normalized -like "*/Models/*.cs" -or $normalized -like "*/DTOs/*.cs") {
        return "Modele de transport : il sert a faire passer des donnees entre couches ou via l'API."
    }
    if ($normalized -like "*/Options/*.cs") {
        return "Options de configuration : cette classe represente des reglages lus depuis la configuration."
    }
    if ($normalized -like "*/Handlers/*.cs") {
        return "Handler d'evenement : il reagit a un evenement emis par une autre partie du systeme."
    }
    if ($normalized -like "*.csproj") {
        return "Fichier projet .NET : il indique le framework, les dependances et les references."
    }
    if ($normalized -like "*.json") {
        return "Fichier de configuration JSON : il fournit des reglages lus au demarrage."
    }
    if ($normalized -like "*.sql") {
        return "Script SQL : il prepare ou modifie la structure et les donnees de la base."
    }
    if ($normalized -like "*.yml" -or $normalized -like "*.yaml") {
        return "Fichier YAML : il decrit une configuration structuree, souvent pour Docker ou l'infrastructure."
    }
    if ($normalized -like "*.http") {
        return "Fichier de tests HTTP : il contient des exemples d'appels d'API."
    }
    if ($normalized -like "*.ps1") {
        return "Script PowerShell : il automatise des actions de developpement ou d'infrastructure."
    }
    if ($normalized -like "*.slnx") {
        return "Solution .NET : elle liste les projets qui composent l'application."
    }

    return "Fichier de code ou de configuration participant au fonctionnement du projet."
}

function Explain-CSharpLine {
    param([string]$Trimmed)

    if ($Trimmed -eq "") { return "Ligne vide : elle sert a aerer le fichier et a separer les blocs." }
    if ($Trimmed.StartsWith("//")) { return "Commentaire : texte destine aux humains, ignore par l'ordinateur." }
    if ($Trimmed.StartsWith("using ")) { return "Importe un espace de noms pour utiliser des classes sans ecrire leur nom complet." }
    if ($Trimmed.StartsWith("namespace ")) { return "Declare le groupe logique dans lequel les classes de ce fichier sont rangees." }
    if ($Trimmed -match "^\[.*\]$") { return "Attribut : ajoute une information speciale lue par .NET ou ASP.NET Core." }
    if ($Trimmed -match "\b(class|record|struct|interface|enum)\b") { return "Declare un type : une definition reutilisable qui organise des donnees ou des comportements." }
    if ($Trimmed -match "^\{+$") { return "Ouvre un bloc : les lignes suivantes appartiennent a l'element precedent." }
    if ($Trimmed -match "^\}+[;,]?$") { return "Ferme un bloc : on sort de la section commencee plus haut." }
    if ($Trimmed -match "\b(public|private|protected|internal)\b.*\b[A-Za-z0-9_<>]+\s+[A-Za-z0-9_]+\s*\(") { return "Declare une methode : une action que le programme pourra appeler." }
    if ($Trimmed -match "\b(get;|set;|init;)\b") { return "Declare une propriete : une donnee lisible ou modifiable sur un objet." }
    if ($Trimmed -match "=>") { return "Utilise une expression flechee : une forme courte pour retourner ou calculer une valeur." }
    if ($Trimmed -match "^\s*if\s*\(") { return "Condition : le code du bloc s'execute seulement si le test est vrai." }
    if ($Trimmed -match "^\s*else\b") { return "Alternative : ce bloc s'execute quand la condition precedente n'est pas satisfaite." }
    if ($Trimmed -match "^\s*(for|foreach|while)\s*\(") { return "Boucle : repete une action plusieurs fois." }
    if ($Trimmed -match "\bawait\b") { return "Attend une operation asynchrone sans bloquer inutilement le serveur." }
    if ($Trimmed -match "^\s*return\b") { return "Renvoie un resultat a l'appelant et termine souvent la methode en cours." }
    if ($Trimmed -match "\bthrow\b") { return "Signale une erreur volontairement pour interrompre le chemin normal." }
    if ($Trimmed -match "\bnew\b") { return "Cree une nouvelle instance d'un objet ou d'une collection." }
    if ($Trimmed -match "\bvar\b") { return "Declare une variable en laissant C# deduire automatiquement son type." }
    if ($Trimmed -match "\bconst\b") { return "Declare une valeur fixe qui ne changera pas pendant l'execution." }
    if ($Trimmed -match "\breadonly\b") { return "Indique qu'une valeur est initialisee puis protegee contre les changements non prevus." }
    if ($Trimmed -match "\bTask\b") { return "Travaille avec une operation asynchrone, typique des appels base de donnees ou reseau." }
    if ($Trimmed -match "\bIEnumerable\b|\bList<|\bDictionary<|\bIReadOnly") { return "Manipule une collection, c'est-a-dire plusieurs elements du meme genre." }
    if ($Trimmed -match "\bMap(Get|Post|Put|Delete|Patch)\b|\bHttp(Get|Post|Put|Delete|Patch)\b") { return "Definit une route HTTP, donc une URL utilisable par un client." }
    if ($Trimmed -match "\bAddScoped\b|\bAddSingleton\b|\bAddTransient\b") { return "Enregistre une dependance pour que .NET puisse la fournir automatiquement." }
    if ($Trimmed -match "\bapp\.Use|\bapp\.Map") { return "Configure le pipeline web : ce que fait le serveur quand une requete arrive." }
    if ($Trimmed -match "\bSELECT\b|\bINSERT\b|\bUPDATE\b|\bDELETE\b") { return "Contient une requete SQL envoyee a la base de donnees." }
    if ($Trimmed -match "^\s*;+$") { return "Termine une instruction C#." }

    return "Instruction de code : elle participe a la logique du fichier."
}

function Explain-XmlLine {
    param([string]$Trimmed)

    if ($Trimmed -eq "") { return "Ligne vide : elle separe visuellement les sections." }
    if ($Trimmed.StartsWith("<!--")) { return "Commentaire XML : information pour les humains, ignoree par les outils." }
    if ($Trimmed -match "<Project") { return "Debut du fichier projet .NET." }
    if ($Trimmed -match "<PropertyGroup") { return "Regroupe des proprietes de configuration du projet." }
    if ($Trimmed -match "<ItemGroup") { return "Regroupe des elements comme dependances ou references de projets." }
    if ($Trimmed -match "<TargetFramework") { return "Indique la version de .NET ciblee par ce projet." }
    if ($Trimmed -match "<PackageReference") { return "Ajoute une bibliotheque externe utilisee par le projet." }
    if ($Trimmed -match "<ProjectReference") { return "Relie ce projet a un autre projet local de la solution." }
    if ($Trimmed -match "</") { return "Ferme une balise XML commencee auparavant." }
    if ($Trimmed -match "<.+>") { return "Balise XML : elle donne une information de configuration structuree." }
    return "Contenu XML de configuration."
}

function Explain-JsonLine {
    param([string]$Trimmed)

    if ($Trimmed -eq "") { return "Ligne vide : elle ameliore la lisibilite." }
    if ($Trimmed -match "^\{|\}$") { return "Accolade JSON : elle ouvre ou ferme un objet de configuration." }
    if ($Trimmed -match "^\[|\]$") { return "Crochet JSON : il ouvre ou ferme une liste de valeurs." }
    if ($Trimmed -match '"ConnectionStrings"') { return "Section contenant les adresses de connexion aux bases ou services." }
    if ($Trimmed -match '"Logging"') { return "Section qui regle les journaux produits par l'application." }
    if ($Trimmed -match '"AllowedHosts"') { return "Reglage ASP.NET Core qui indique les noms d'hote autorises." }
    if ($Trimmed -match '"[^"]+"\s*:') { return "Propriete JSON : une cle associee a une valeur de configuration." }
    return "Element JSON de configuration."
}

function Explain-SqlLine {
    param([string]$Trimmed)

    if ($Trimmed -eq "") { return "Ligne vide : elle separe les blocs SQL." }
    if ($Trimmed.StartsWith("--")) { return "Commentaire SQL : explication ignoree par la base de donnees." }
    if ($Trimmed -match "CREATE TABLE") { return "Cree une table, c'est-a-dire une structure de stockage de donnees." }
    if ($Trimmed -match "CREATE INDEX") { return "Cree un index pour accelerer certaines recherches." }
    if ($Trimmed -match "ALTER TABLE") { return "Modifie une table existante." }
    if ($Trimmed -match "INSERT INTO") { return "Ajoute des lignes de donnees dans une table." }
    if ($Trimmed -match "PRIMARY KEY") { return "Definit l'identifiant unique d'une ligne." }
    if ($Trimmed -match "FOREIGN KEY|REFERENCES") { return "Cree un lien entre deux tables." }
    if ($Trimmed -match "NOT NULL") { return "Interdit l'absence de valeur dans cette colonne." }
    if ($Trimmed -match "DEFAULT") { return "Definit une valeur automatique si aucune valeur n'est fournie." }
    if ($Trimmed -match "SELECT|UPDATE|DELETE") { return "Requete SQL qui lit ou modifie des donnees." }
    return "Instruction SQL utilisee pour preparer ou manipuler la base."
}

function Explain-YamlLine {
    param([string]$Trimmed)

    if ($Trimmed -eq "") { return "Ligne vide : elle rend la configuration plus lisible." }
    if ($Trimmed.StartsWith("#")) { return "Commentaire YAML : texte explicatif ignore par l'outil." }
    if ($Trimmed -match "^[A-Za-z0-9_-]+:") { return "Cle de configuration YAML : elle ouvre ou definit une section." }
    if ($Trimmed -match "^- ") { return "Element de liste YAML." }
    return "Valeur YAML rattachee a la section courante."
}

function Explain-HttpLine {
    param([string]$Trimmed)

    if ($Trimmed -eq "") { return "Ligne vide : elle separe les requetes ou leurs sections." }
    if ($Trimmed.StartsWith("###")) { return "Separateur de requete dans un fichier de tests HTTP." }
    if ($Trimmed.StartsWith("@")) { return "Variable reutilisable dans les requetes HTTP du fichier." }
    if ($Trimmed -match "^(GET|POST|PUT|DELETE|PATCH)\s+") { return "Requete HTTP envoyee vers une route de l'API." }
    if ($Trimmed -match "^[A-Za-z-]+:") { return "En-tete HTTP : information technique jointe a la requete." }
    if ($Trimmed -match "^\{|\}$") { return "Corps JSON envoye avec la requete HTTP." }
    return "Ligne participant a un exemple d'appel API."
}

function Explain-PowerShellLine {
    param([string]$Trimmed)

    if ($Trimmed -eq "") { return "Ligne vide : elle separe les etapes du script." }
    if ($Trimmed.StartsWith("#")) { return "Commentaire PowerShell : aide humaine ignoree a l'execution." }
    if ($Trimmed -match "^\$") { return "Variable PowerShell : elle stocke une valeur reutilisable." }
    if ($Trimmed -match "function ") { return "Declare une fonction reutilisable dans le script." }
    if ($Trimmed -match "param\(") { return "Declare les parametres attendus par le script ou la fonction." }
    if ($Trimmed -match "if\s*\(") { return "Condition : execute un bloc seulement si le test est vrai." }
    if ($Trimmed -match "foreach\s*\(") { return "Boucle : repete une action pour chaque element." }
    return "Commande PowerShell executee par le script."
}

function Explain-Line {
    param(
        [string]$Path,
        [string]$Line
    )

    $trimmed = $Line.Trim()
    switch ([System.IO.Path]::GetExtension($Path).ToLowerInvariant()) {
        ".cs" { return Explain-CSharpLine $trimmed }
        ".csproj" { return Explain-XmlLine $trimmed }
        ".slnx" { return Explain-XmlLine $trimmed }
        ".json" { return Explain-JsonLine $trimmed }
        ".sql" { return Explain-SqlLine $trimmed }
        ".yml" { return Explain-YamlLine $trimmed }
        ".yaml" { return Explain-YamlLine $trimmed }
        ".http" { return Explain-HttpLine $trimmed }
        ".ps1" { return Explain-PowerShellLine $trimmed }
        default {
            if ($trimmed -eq "") { return "Ligne vide : elle ameliore la lisibilite." }
            return "Ligne de contenu du fichier."
        }
    }
}

New-Item -ItemType Directory -Force -Path (Split-Path $OutputMarkdown) | Out-Null

$files = Get-ChildItem -Path $Root -Recurse -File |
    Where-Object {
        $extension = $_.Extension.ToLowerInvariant()
        if ($codeExtensions -notcontains $extension) { return $false }
        $fullName = $_.FullName
        foreach ($part in $excludedPathParts) {
            if ($fullName -like "*$part*") { return $false }
        }
        return $true
    } |
    Sort-Object FullName

$totalLines = 0
foreach ($file in $files) {
    $totalLines += (Get-Content -LiteralPath $file.FullName).Count
}

$tick = [char]96
$markdown = [System.Collections.Generic.List[string]]::new()
$markdown.Add("---")
$markdown.Add("title: Cours AppAnimaux - lecture ligne par ligne")
$markdown.Add("author: Generation automatique locale")
$markdown.Add("date: $(Get-Date -Format 'yyyy-MM-dd')")
$markdown.Add("geometry: margin=1.4cm")
$markdown.Add("fontsize: 9pt")
$markdown.Add("documentclass: article")
$markdown.Add("toc: true")
$markdown.Add("---")
$markdown.Add("")
$markdown.Add("# Cours AppAnimaux - lecture ligne par ligne")
$markdown.Add("")
$markdown.Add("Ce document transforme le code du projet AppAnimaux en support de cours pour debutants. Il explique le role de chaque fichier puis commente chaque ligne avec des mots simples.")
$markdown.Add("")
$markdown.Add("> Important : les commentaires sont pedagogiques. Ils expliquent l'intention probable d'une ligne d'apres sa forme et son contexte technique, sans remplacer une revue humaine du metier exact.")
$markdown.Add("")
$markdown.Add("## Vue d'ensemble")
$markdown.Add("")
$markdown.Add("- Fichiers analyses : $($files.Count)")
$markdown.Add("- Lignes commentees : $totalLines")
$markdown.Add("- Racine du projet : " + $tick + $Root.Replace('\', '/') + $tick)
$markdown.Add("")
$markdown.Add("## Comment lire ce cours")
$markdown.Add("")
$markdown.Add("Chaque section correspond a un fichier. Pour chaque fichier, vous trouverez son role general, puis une liste de lignes. Le numero indique la position dans le fichier, le texte entre apostrophes inverses montre le code original, et l'explication decrit ce que cette ligne fait.")
$markdown.Add("")

foreach ($file in $files) {
    $relativePath = [System.IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
    $language = Get-LanguageName $file.FullName
    $lines = Get-Content -LiteralPath $file.FullName

    $markdown.Add("\newpage")
    $markdown.Add("")
    $markdown.Add("## $relativePath")
    $markdown.Add("")
    $markdown.Add("**Role du fichier :** $(Get-FileRole $relativePath)")
    $markdown.Add("")
    $markdown.Add("**Nombre de lignes :** $($lines.Count)")
    $markdown.Add("")
    $markdown.Add('```' + $language)
    foreach ($line in $lines) {
        $markdown.Add($line)
    }
    $markdown.Add('```')
    $markdown.Add("")
    $markdown.Add("### Commentaire ligne par ligne")
    $markdown.Add("")

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $number = $i + 1
        $code = Escape-InlineCode $lines[$i]
        $explanation = Explain-Line $file.FullName $lines[$i]
        $markdown.Add("- Ligne $number : " + $tick + $code + $tick + " - " + $explanation)
    }

    $markdown.Add("")
}

[System.IO.File]::WriteAllLines($OutputMarkdown, $markdown, [System.Text.UTF8Encoding]::new($false))

$pandoc = Get-Command pandoc -ErrorAction SilentlyContinue
if ($pandoc) {
    & $pandoc.Source $OutputMarkdown `
        --from markdown+yaml_metadata_block `
        --pdf-engine=xelatex `
        --toc `
        --number-sections `
        -V colorlinks=true `
        -V linkcolor=blue `
        -V urlcolor=blue `
        -o $OutputPdf
}
else {
    Write-Warning "Pandoc est introuvable. Le fichier Markdown a ete genere, mais pas le PDF."
}

Write-Host "Markdown : $OutputMarkdown"
if (Test-Path -LiteralPath $OutputPdf) {
    Write-Host "PDF : $OutputPdf"
}





