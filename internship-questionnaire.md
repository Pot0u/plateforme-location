# Questionnaire Technique — Stage Développeur Full Stack

> Ce questionnaire évalue ta capacité à lire et comprendre une base de code existante.
> Il n'y a pas de piège : prends le temps d'explorer le dépôt avant de répondre.
> Les réponses courtes et précises sont préférées aux réponses longues et vagues.

---

## Partie 1 — Architecture générale (niveau débutant)

**Q1.** Le code backend est organisé en dossiers `Features/` à l'intérieur de chaque module, plutôt qu'en dossiers `Controllers/`, `Services/`, `Repositories/` comme on le voit souvent dans les tutoriels.

Pourquoi ce choix ? 

```
(Il faut savoir que je n'ai vu aucun des 2, seulement le MVC)
L'organisation en `Features/` regroupe par fonctionnalité dans un seul dossier, comme dans ce projet les Login/Resgister. 
L'autre méthode regroupe par code de type technique. 
Donc `Features/` rend le code plus facile à lire et à modifier car tout se trouve dans le même dossier.

```

Quel est l'avantage concret pour un développeur qui travaille sur une seule fonctionnalité ?

```
L'avantage est pour un développeur qui travaille sur une seule fonctionnalité et qu'il trouve tout dans son dossier.


```

---

**Q2.** Le projet contient deux modules distincts : `Customers` et `DiscogsImportation`. Chacun a son propre `DbContext`.

Pourquoi ne pas utiliser un seul `DbContext` partagé pour toute l'application ?

```
Avoir un `DbContext` par module permet de gérer leurs BD chacun de leur côté.
S'il y en avait qu'un seul, il aurait géré toutes les tables de l'application.
Donc on évite pour ne pas avoir un gros fichier, et ça aurait été difficile à maintenir.
(Je pense)

```

---

**Q3.** Dans le module `DiscogsImportation`, il existe une interface `IDiscogsClient` avec deux implémentations : `FakeDiscogsClient` et `DiscogsApiClient`.

a) Où est décidé laquelle des deux est utilisée à l'exécution ?

```
Dans `program.cs`
// Use FakeDiscogsClient in Development, real client in Production
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IDiscogsClient, FakeDiscogsClient>();
}
else
{
    builder.Services.AddHttpClient<IDiscogsClient, DiscogsApiClient>();
}


```


b) Pourquoi ce mécanisme plutôt que d'appeler directement l'API Discogs partout dans le code ?

```
Pour quand on fait des tests on utilise le fake, et quand il est en production on utilise le vrai.
Et on l'utilise dans program.cs pour le faire dans un seul endroit pour éviter des if/else partout dans l'API discogs.


```
---

## Partie 2 — Flux d'une requête (niveau intermédiaire)

**Q4.** Trace le chemin complet d'une requête HTTP `POST /api/discogs/import/master/1234` :

- Quel fichier reçoit la requête en premier ?
```



```
- Que se passe-t-il ensuite (sans décrire le code ligne par ligne — décris les étapes logiques) ?
```



```
- Quel objet est retourné au client ? Comment le client peut-il utiliser cet objet ?
```





```

---

**Q5.** Dans les handlers Wolverine, les dépendances (`IDiscogsClient`, `DiscogsDbContext`, etc.) sont passées en **paramètres de méthode**, pas via un constructeur.

Est-ce que tu reconnaîs ce mécanisme ? Comment s'appelle-t-il et pourquoi est intéressant ici ?
```






```

---

**Q6.** Que fait concrètement cette ligne dans `Program.cs` :

```csharp
opts.UseEntityFrameworkCoreTransactions();






```

Quel problème cela résout-il sans que le développeur ait à y penser ?
`
```



````

---

## Partie 3 — Données et persistance (niveau intermédiaire)

**Q7.** Les identifiants des entités (ex: `MasterRelease.Id`) sont de type `Ulid` et non `Guid`.

a) Quelle est la différence principale entre un ULID et un GUID ?

```



```

b) Pourquoi ce choix peut être intéressant pour une base de données ?

```



```

---

**Q8.** Dans la configuration EF Core du module `DiscogsImportation`, on voit :

```csharp
masterRelease.Property(m => m.Genres)
    .HasColumnType("jsonb");
```

Qu'est-ce que `jsonb` dans PostgreSQL ?
```



```


Pourquoi stocker les genres ainsi plutôt que dans une table séparée ?
```



```

---

**Q9.** Dans la configuration EF Core, certaines propriétés sont mappées avec `OwnsMany()` :

```csharp
masterRelease.OwnsMany(m => m.Tracklist, track => { ... });
```

Qu'est-ce qu'un "Owned Type" en EF Core ? 
```



```


Quel est l'avantage par rapport à une entité indépendante avec sa propre table ?
```



```

---

## Partie 4 — Tests (niveau intermédiaire)

**Q10.** Le projet définit plusieurs fixtures de test : `DiscogsReadOnlyFixture`, `DiscogsIsolatedFixture`, `DiscogsErrorCaseFixture`.

Pourquoi avoir trois fixtures différentes plutôt qu'une seule ? 

```



```

Explique dans quel cas tu utiliserais chacune.

```



```
---

**Q11.** Les collections XUnit sont définies avec `nameof()` :

```csharp
[CollectionDefinition(nameof(CustomersCollection))]
public class CustomersCollection : ICollectionFixture<CustomersFixture> { }
```

Pourquoi utiliser `nameof()` ici plutôt qu'une chaîne de caractères en dur comme `"CustomersCollection"` ?
```



```

---

**Q12.** Dans les tests, on utilise **Alba** pour appeler les endpoints HTTP plutôt qu'un `HttpClient` classique.

Quelle est la différence fondamentale ?
```



```

Pourquoi Alba est-il plus adapté aux tests d'intégration dans ce contexte ?
```



```

---

## Partie 5 — Gestion des erreurs (niveau avancé)

**Q13.** Le handler `ImportMasterRelease` utilise un objet `DiscogsResult<T>` pour représenter le résultat de l'appel à l'API Discogs.

a) Quels sont les cas d'erreur possibles (regarde le code) ?
```



```
b) Quelle est la différence entre cette approche et lever une exception directement ?
```



```
c) Quel avantage concret pour le code appelant ?
```



```
---

**Q14.** Dans ce projet, les handlers ne contiennent **aucun `try/catch`**. Pourtant des erreurs peuvent survenir (réseau, base de données, etc.).

Comment sont-elles gérées ? Qui s'en occupe  ou devrait s'en occuper?
```



```

---

## Partie 6 — HATEOAS et design API (niveau avancé)

**Q15.** Les réponses de l'API contiennent systématiquement un objet `_links`. Par exemple :

```json
{
  "id": "...",
  "title": "The Dark Side of the Moon",
  "_links": {
    "self": "/api/discogs/masters/...",
    "releases": "/api/discogs/releases?masterId=..."
  }
}
```

a) Comment s'appelle ce principe de design d'API ?
```



```

b) Quel est l'avantage pour le frontend Angular qui consomme cette API ?
```



```

---

## Partie 7 — Feature à implémenter (pratique)

> Cette dernière partie est intentionnellement ouverte. Il n'y a pas une seule bonne réponse.
> On évalue ta façon de raisonner, pas la perfection du code.

---

**Q16.** Le module `DiscogsImportation` permet déjà de chercher des releases par genre (`GetReleasesByGenre`) ou par artiste (`GetReleasesByArtist`).

**Implémente ou pseudo-code une nouvelle feature : `GetReleasesByLabel`** — retourner la liste des releases associées à un label donné (ex : "Blue Note Records"), avec pagination.

Tu peux t'inspirer librement des features existantes. On attend :

1. La définition du message (record `GetReleasesByLabel`)
2. La signature du handler et sa logique principale (en pseudo-code ou vrai C# — au choix)
3. La définition de l'endpoint (route, méthode HTTP, paramètres)
4. Les `_links` que tu inclurais dans la réponse

**Bonus** : Que faudrait-il vérifier ou tester en priorité pour valider cette feature ?

```



```

---

*Bonne chance — et n'hésite pas à expliquer ton raisonnement même quand tu n'es pas sûr(e) de la réponse.*
