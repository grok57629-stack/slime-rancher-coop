# Audit de stabilité SRMP — mise à jour du 25 septembre 2026

**État actuel : build 0.0.0.1652, compilation SRML réussie et 160 vérifications automatisées.** Les nouveaux journaux montrent que le gold slime a déclenché la même entrée alimentaire périmée que les autres slimes, pas un défaut propre à son IA. Le build 1651 filtre désormais ces références. Le build 1652 durcit aussi le despawn réseau : une animation FX invalide ne peut plus interrompre la suppression de l'acteur, et les handlers host/client passent par le même nettoyage. La DLL 1652 est installée dans la copie portable. Les frame-times et une session multijoueur réelle restent à vérifier.

## Résultats du nouvel audit

- Les 57 CS3021 provenaient de `[CLSCompliant(false)]` redondants dans le Lidgren embarqué. J'ai retiré 60 attributs dans 12 fichiers source, y compris les variantes de plateforme non compilées ici. La compilation SRML ne masque aucun code d'avertissement et donne maintenant zéro avertissement C#.
- Le handshake faisait confiance au GUID local envoyé par le client, puis l'hôte l'utilisait comme nom du fichier de position. Un client pouvait ainsi faire lire/écraser le fichier de position d'une autre identité. L'hôte consomme encore les 16 octets historiques pour garder le parseur aligné, les ignore et dérive le GUID de sauvegarde depuis le Product User ID EOS authentifié avec SHA-256. Les anciennes sauvegardes de position avec GUID client ne sont pas supprimées, mais ne sont pas reprises automatiquement.
- L'AutoHost ne suspendait pas le monde vide, restait parfois vivant après un échec de démarrage, et pouvait laisser le wrapper attendre indéfiniment devant un arrêt propre si le temps Unity était gelé. Il sauvegarde maintenant à la déconnexion du dernier client, met `Time.timeScale` à zéro tout en laissant les ticks EOS/réseau fonctionner, reprend à l'arrivée d'un client, quitte après un échec transitoire pour que le lanceur réessaie, et utilise des attentes non dépendantes du temps Unity pour l'arrêt.
- Les opérateurs AutoHost étaient sélectionnés par pseudo contrôlé par le client. La liste utilise maintenant des Product User IDs EOS, et `/id` révèle à chaque joueur son identifiant authentifié pour configurer la liste.
- `EpicApplication.Shutdown` pouvait déréférencer un lobby nul et appeler EOS Shutdown après une initialisation ratée. Les interfaces sont maintenant contrôlées avant libération ; `AlreadyConfigured` reste accepté sans arrêter l'initialisation EOS d'un autre mod.
- À la déconnexion EOS, le callback d'expiration du jeton était désinscrit avec `RemoveNotifyLoginStatusChanged` au lieu de `RemoveNotifyAuthExpiration`. Le handle natif restait enregistré ; chaque callback est maintenant retiré par son API correspondante, vérifiée dans l'IL compilé.
- L'installateur plaçait les anciennes DLL sous `SRML\Mods\SRMP-Backups`. SRML les charge récursivement et signalait alors un mod `srmp` dupliqué au lancement suivant. Les sauvegardes vont maintenant dans `SRML\Backups\SRMP` ; les anciens fichiers imbriqués sont migrés et la désinstallation sait aussi les restaurer.
- `serveur/` contient un lanceur Windows qui installe la DLL/config et supervise l'hôte, et un lanceur Linux pour le jeu Windows sous Proton. Le processus reste le jeu Unity sans affichage : cette base ne simule pas Slime Rancher dans un daemon autonome. Le mode natif Linux et Raspberry Pi ARM ne sont pas pris en charge par la DLL EOS Windows x64 actuelle.

`scripts/Test-SRMP.ps1` réussit sur le build 1652 : **60 vérifications de transport/identités/sauvegardes + 90 vérifications d'assembly/Harmony + 10 tests d'installation/désinstallation = 160**. Les nouveaux tests contrôlent le garde alimentaire, les appels des handlers de destruction au nettoyage central et leurs signatures IL. Le smoke test Unity réel précédent confirme que SRML et SRMP 1637 se chargent et que la connexion EOS Device ID aboutit. Il s'arrête avant de charger un monde. Le journal contient également une erreur native « AUTH_PASSWORD » avant la connexion Device ID réussie ; elle n'a pas empêché l'authentification et reste à comparer sur une vraie session.

Une limite de sécurité subsiste : `EpicApplication` contient encore le Client Secret EOS fourni par le dépôt public. Epic décrit ce secret comme le mot de passe qui valide le Client ID ([documentation EOS](https://dev.epicgames.com/documentation/unreal-engine/enable-and-configure-online-services-eos-in-unreal-engine)). Il faut le révoquer/remplacer dans le portail EOS par le propriétaire du produit avant une distribution publique ; cette rotation ne peut pas être faite depuis ce dépôt.

## Architecture examinée

- `MainSRML.Load` charge le profil, crée les composants persistants et installe les patches Harmony. Le chargement du profil est partagé avec Standalone, dont la compilation n'a pas été validée ici.
- `EpicApplication` / `EpicLobby` gèrent EOS ; `EpicP2P` transporte les messages. Les classes actives sont `EOS/EpicNetworkClient.cs` et `EOS/EpicNetworkServer.cs`. Les anciennes classes `NetworkClient2` / `NetworkServer2` ne sont pas le transport EOS utilisé.
- Le handshake échange version, UUID, mods, VR et DLC, puis la liste des joueurs, l'identité de l'hôte et le monde. Les snapshots du monde/ranch/gadgets/upgrades précèdent la notification de fin de chargement ; les acteurs sont envoyés à l'entrée dans les régions.
- Le serveur arbitre les propriétaires. Le propriétaire d'un acteur le simule ; celui d'une région simule ses drones et collecteurs. Les Gordos consomment désormais sur l'hôte. Les patches émettent les changements ; les handlers les appliquent sous une portée `HandlePacket` qui empêche leur réémission.
- Le monde reste sauvegardé par l'hôte. Les positions des joueurs sont enregistrées par UUID côté hôte, et l'inventaire personnel côté client.

L'audit a couvert le transport, les handlers, les propriétaires et registres, le chargement/reconnexion, de nombreux patches de gameplay et le build. Des méthodes des assemblies du jeu ont été décompilées pour vérifier les signatures et effets de bord. Cela ne constitue pas une lecture exhaustive du SDK EOS généré, ni une validation de toutes les combinaisons de mods tiers.

## Défauts démontrés et corrections

| Domaine | Cause constatée | Correction |
|---|---|---|
| Transport | Changements d'état non fiables par défaut ; fragmentation mélangeant des messages de même type ; capacité du buffer utilisée au lieu de sa longueur ; compteur sur un octet. | Framing commun, identifiants de messages, réassemblage distinct par pair, compteur entier et longueur exacte. État fiable/ordonné ; mouvement explicitement non fiable. |
| Fragments | Doublons, messages intercalés et incomplets pouvant corrompre le contenu ou retenir de la mémoire. | Validation, déduplication, limite de 16 Mio par message, 32 messages incomplets / 32 Mio par pair, expiration des fragments incomplets. |
| Connexion | Réception précédant le callback d'établissement ; notification répétée recréant un joueur ; callbacks tardifs touchant une nouvelle session. | Callbacks avant réception, initialisation/arrêt idempotents, générations des opérations de lobby, handles libérés et références invalidées avant fermeture. |
| Reconnexion | Propriétaires et connaissances d'acteurs conservés après départ, références de lobby bloquant le prochain join. | Nettoyage des pairs, régions et `KnownPlayers`, reprise d'autorité, fermeture locale même après erreur ; timeout d'authentification. |
| Identifiants | IDs d'acteurs aléatoires pouvant entrer en collision ; hash de chaîne non contractuel entre processus. | Namespace d'acteurs par créateur, curseur au join et hash FNV-1a spécifié pour les chemins de scène. |
| Chargement tardif | Application avant disponibilité de la scène ; notification de joueur chargé perdue ; régions actives non annoncées en fin de chargement. | File bornée avant chargement, notification conservée, annonces des régions actives et garde contre un `PlayerLoaded` répété. |
| DLC | Le handshake EOS n'envoyait pas les DLC à comparer. | Liste reconstruite à chaque authentification, comparaison d'ensembles, logs des différences. La raison de rejet est envoyée avant la fermeture, avec timeout de secours. |
| Portées réseau | Un retour anticipé ou une exception laissait `HandlePacket` actif ; finalizers incompatibles avec l'imbrication. | `try/finally` direct restaurant la valeur précédente, testé sur retour, erreur et imbrication. |
| Exceptions silencieuses | L'extension d'invocation SRML absorbait les erreurs des handlers personnalisés. | Réflexion explicite laissant remonter l'erreur au journal réseau. |
| Autorité | Prises spéculatives/récursives, demandes répétées, IDs de déplacement/animation fournis par l'émetteur. | Attente de confirmation du serveur, distinction demande passive/interaction, validation du propriétaire et des IDs, identité tirée de la connexion. |
| Mouvement | Pose initiale nulle, dernier paquet perdu sans rattrapage, évaluations trop fréquentes. | Pose initialisée, cadence de 10 Hz et rappel stationnaire de 1 Hz ; rotations comparées correctement. |
| Slimes/plorts | Consommation possible par une réplique ; nourriture appartenant à un autre pair. | Garde des points de départ/fin du repas et transfert confirmé de nourriture avant consommation. |
| Recherche de nourriture / FPS | `FindConsumable` conservait des références vers des GameObjects détruits ; `Identifiable.IsEdible` appelait `GetComponent` sans vérifier la référence Unity. Les exceptions se répétaient dans `StalkConsumable.Relevancy` et `GatherIdentifiableItems.Relevancy` pendant les ticks des slimes. | Patch Harmony : les objets Unity nuls ou détruits sont simplement écartés comme non comestibles. Teste le résultat du garde sans moteur Unity ; la baisse réelle des frame-times reste à mesurer en jeu. |
| Despawn d'acteur interrompu | Les journaux pré-1651 montrent des `ActorDestroy` qui échouaient dans `NetworkActor.OnDestroyEffect` avant le `Destroyer.DestroyActor` et le retrait du registre ; une FX ou référence Unity invalide pouvait donc laisser une réplique persistante. | Build 1652 : nettoyage central partagé client/serveur, FX en best effort, despawn de secours et retrait du registre en `finally`. Les deux handlers sont vérifiés dans l'IL ; le cas Unity réel reste à tester en session. |
| Snapshots d'acteurs | Décoder une date de plort créait un objet Unity ; échec partiel laissant un acteur sans enregistrement réseau. | Date nullable sans `GameObject` au décodage ; destruction de l'acteur partiellement initialisé en cas d'erreur. |
| Émotions/feral | Peur/faim décodées avec l'enum agitation ; tick de feralité exécuté sur les répliques ; aura recréée. | Enums corrects, tick limité au propriétaire, émission autoritaire et application idempotente du statut feral. |
| Gordos | Deltas et éclatements répartis entre clients, donc compteur/récompenses dépendant de l'ordre des repas. Coroutine arrêtée par une région inactive. | Requête avec ID de nourriture, consommation/récompenses hôte, compteur absolu acquitté, effet visuel gardé. Coroutine sur contexte persistant, annulée si session/Gordo disparaît. |
| Upgrades | Remplacement de `List<Upgrade>.Add` par une méthode statique ne consommant qu'un argument au lieu de deux. | Signature correcte, ajout conservé, labels préservés ; IL produit réellement exécuté par le test. |
| Drones | Région absente à l'abonnement, propriété acquise avant `Start`, station recherchée dans une mauvaise branche de la hiérarchie. | Initialisation défensive, réévaluation si déjà propriétaire, résolution via `DroneGadget`, émission par le propriétaire. |
| Inventaire drone | `MaybeAddToSlot` n'envoyait aucun ajout drone ; quantités reçues forcées à 1. | Observation de la méthode du jeu au lieu de sa réécriture, quantité réelle et débordement transmis. |
| Plort Collector | Activation distante ne rejoignant pas l'autorité, exécution sous suppression réseau, anciens joints conservés après transfert. | Requête routée au propriétaire, exécution autoritaire hors replay, anciens joints libérés. Attente de propriété avant stockage/destruction d'un plort déjà à l'entrée. |
| Gadgets/stutters | Cache de tourelles obsolète après remplacement, rotations envoyées chaque frame, annonces de région lors d'un replay de gadget. | Cache invalidé, index vérifiés, cadence limitée ; suppression des annonces provoquées par le replay. Aucun gain chiffré de FPS revendiqué. |
| Nutcracker | Prefix ne renseignant pas son résultat `IEnumerator` ; référence à la nourriture après sa destruction. | Coroutine retournée, références vérifiées et pose capturée avant l'attente. |
| Argent | Total autoritaire envoyé à tous sauf l'émetteur, qui pouvait rester désynchronisé après des opérations croisées. | Total envoyé à tous ; origine transmise pour éviter le double popup. |
| Sauvegardes | Restauration d'inventaire dépendant d'un paquet de position ; pose interpolée non restaurée ; fichiers tronquables. | Restauration à la fin de l'initialisation, pose/inclinaison corrigées, parsing complet avant application, écritures atomiques et `.bak`. |
| Mondes homonymes | Même fichier `.player` local pour deux hôtes ayant le même nom de monde. | Clé UUID hôte + hash du monde ; migration unique de l'ancien fichier, original renommé conservé. |
| Identité de sauvegarde joueur | Le client choisissait le GUID du fichier de position persistant côté hôte. | L'hôte ignore le GUID client et dérive une identité stable du Product User ID EOS authentifié. |
| Premier lancement | JSON invalide ou UUID vide interrompant le chargement ou fournissant une identité nulle. | Chargement commun, conservation du fichier invalide, récupération du backup si possible, identité valide persistée. |
| Chat | Ouverture différée, vieillissement dans `OnGUI` appelé plusieurs fois par frame, commandes hôte envoyées comme texte, paquets blancs préfixés. | Flux IMGUI commun pour ouvrir/envoyer, expiration dans `Update`, remise à zéro à la sortie, commandes hôte routées, rejet du blanc et limite de longueur. |
| Spirit | Méthodes masquant les sérialiseurs virtuels et ajoutant un format après le format générique. | Overrides et un seul format, test via `IPacket`. HolySlimes reste non validé en jeu. |
| Build | Dépendance à Visual Studio, références Harmony incohérentes, absence de SRML bloquant même la compilation. | SDK .NET pris en charge, Harmony 2.2.1 et reference assemblies restaurées, chemins paramétrés, package avant vérification d'installation. |
| EOS callbacks | Le handle de notification d'expiration était envoyé à l'API de retrait du statut de connexion. | Chaque notification EOS est maintenant retirée avec son API correspondante ; l'IL construit est vérifié par test. |
| Installateur SRML | Le rollback DLL dans `Mods\SRMP-Backups` était rechargé par SRML comme une seconde copie du mod. | Sauvegardes sous `SRML\Backups\SRMP`, migration de l'ancien dossier, test d'installation/désinstallation. |
| AutoHost distant | Pas de pause à vide, échec de démarrage laissant le processus actif, arrêt ralenti bloqué par `WaitForSeconds`, droits opérateur basés sur pseudo. | Pause/reprise selon le nombre de connexions authentifiées, sauvegarde à vide, échec fatal pour redémarrage, attente en temps réel, allowlist EOS par Product User ID. |

## Validation effectuée

`scripts/Test-SRMP.ps1` a réussi : **47 vérifications transport/identités/sauvegardes + 62 vérifications de l'assembly = 109**. `scripts/Test-Installer.ps1` ajoute **8 vérifications** de migration, remplacement et rollback ; total **117**.

- Transport : tailles de 1 à 1 048 577 octets, dépassement de 255 fragments, intercalage, doublons, perte/expiration, entêtes invalides et limites d'allocation.
- Persistance : séparation hôte/monde, stabilité après reconnexion, écritures initiales/remplacements, interruption simulée, maintien des versions et reprise.
- Assembly produite : aller-retour des paquets modifiés, snapshot de plort sans objet Unity, portées des handlers, exécution de l'IL des upgrades, résolution/signatures Harmony et réécriture du lancement de coroutine Gordo.
- Deux wrappers Harmony managés ont été construits. Les ECalls Unity empêchent de construire tous les wrappers sous le CLR Windows sans le runtime du jeu : une signature valide ne prouve pas l'exécution du gameplay.
- Compilations `SRML NoVer` après les changements importants, puis **`SRML` avec T4 réussie**. Le build 1632 avait 57 CS3021 ; ils ont été éliminés avant le build 1637. La compilation finale affiche **zéro avertissement C# et zéro erreur**.
- Smoke test du build 1637 sous Windows : SRML et SRMP chargés, connexion EOS Device ID réussie, démarrage arrêté avant chargement d'un monde sentinelle inexistant. Journal : `artifacts/headless-smoke-1637-20260923.log`.

## Artefacts initiaux (build 1632)

Commande exécutée : `Build SRMP.cmd -Version stability-audit`, qui appelle `scripts/Build-SRMP.ps1 -Install -Package`.

- Version : **0.0.0.1632** ; modinfo **0.0.1632**.
- DLL : `Builds/SRMP/SRMP.dll`.
- SHA256 DLL : `7340FD0D20FECECE94CFDC3A0C7D43FAD3ABAB06FB695B1D72DCEB870027F461`.
- Package : `dist/SRMP-Revival-stability-audit-Windows.zip` ; checksum adjacent.
- Jeu détecté : `C:\Program Files\Epic Games\SlimeRancher`.
- **Installation bloquée** : absence de `C:\Program Files\Epic Games\SlimeRancher\SRML\Mods`. La commande globale retourne donc 1 après avoir compilé et créé le package. La DLL n'est pas annoncée comme installée.
- SDK 8.0.425 utilisé depuis `artifacts/tools/dotnet/dotnet.exe` ; `dotnet-t4` 3.0.0 installé.
- **Protocole modifié : tous les participants doivent utiliser exactement cette DLL.** Incompatibilité avec les anciens paquets EOS SRMP.

## Artefacts build 1637

- Version assembly/modinfo : **0.0.0.1637 / 0.0.1637**.
- DLL compilée et installée : `Builds/SRMP/SRMP.dll` ; SHA256 `7D93035F07ACDA4B3781422B35BCCB6EF11B926559D0067ECE48BE66C8F9DF35`.
- Paquet : `dist/SRMP-Revival-stable-1637-Windows.zip` ; son checksum adjacent est `dist/SRMP-Revival-stable-1637-SHA256.txt`.
- Commande : `scripts/Build-SRMP.ps1 -Configuration SRML -Install -Package -Version stable-1637`. Installation locale confirmée dans `C:\Program Files\Epic Games\SlimeRancher\SRML\Mods\SRMP.dll` avec le même hash.
- SRML v0.2.1c officiel a été installé pour le smoke test ([release SRML](https://github.com/SlimeRancherModding/SRML/releases)). L'assembly de jeu d'origine est sauvegardée dans `artifacts/game-backup-20260923/Assembly-CSharp.dll`.

## Essais réels et problèmes encore ouverts

Les étapes et résultats attendus sont dans `TESTING.md`. Aucun cas multijoueur n'est coché comme validé par cet audit.

1. Deux puis trois joueurs : repas/transformations, Gordos sauvages/piégés hors de la région de l'hôte, collecteurs, changements de propriétaire pendant une consommation et reconnexions répétées.
2. DLC au premier lancement sans ouvrir leur menu : le handshake est corrigé, mais l'initialisation des entitlements du jeu/EOS n'a pas été mesurée.
3. Drones sur une longue session avec sommeil, changements de cellule et remplacement de gadget. Tous leurs états internes et temporisateurs ne forment pas encore un snapshot transactionnel complet.
4. Achats, clés, silos/refinery et autres ressources partagées utilisent encore en partie des mutations optimistes/deltas. Le total d'argent converge ; aucune transaction hôte avec rollback n'empêche encore deux achats concurrents depuis le même solde.
5. L'inventaire personnel reste stocké côté client. La migration d'un ancien fichier homonyme l'attribue au premier hôte rejoint avec ce nom ; son original renommé est conservé.
6. L'expiration feral et les transitions pendant un transfert d'autorité nécessitent des essais : les champs corrigés ne prouvent pas la synchronisation de chaque temporisateur interne.
7. Compteurs/sorties exacts du Nutcracker, tous les upgrades, anciennes sauvegardes et mods tiers. Les identifiants statiques supposent des hiérarchies identiques ; une collision de hash n'est pas mathématiquement impossible.
8. Profilage du placement des gadgets et d'un ranch chargé : les instanciations et snapshots volumineux peuvent encore provoquer des pauses.
9. Les anciens fichiers de position des joueurs utilisaient le GUID fourni par le client. Ils restent sur disque, mais le nouveau calcul basé sur EOS ne migre pas ces positions.
10. Le Client Secret EOS embarqué doit être renouvelé par le propriétaire du produit avant distribution.
11. La connexion EOS Device ID a été testée lors du smoke headless. La création du lobby, un vrai chargement de monde, le wrapper Proton Linux et toute session à deux clients restent à valider.
12. Le journal EOS affiche un diagnostic natif « Failed to get command line argument 'AUTH_PASSWORD' » avant une authentification Device ID réussie ; son origine et son caractère attendu restent à confirmer sur une session complète.

### Itération du 24 septembre — build 1642

Le nouvel audit a suivi les mises à jour de position des acteurs : chaque slime/objet mobile pouvait auparavant créer son propre envoi EOS toutes les 100 ms, puis le serveur retransmettait chaque paquet séparément. Le build 1642 regroupe maintenant les positions modifiées pendant une frame en paquets non fiables de 20 acteurs maximum. Le serveur n'applique et ne retransmet que les acteurs dont l'expéditeur possède l'autorité. La fréquence d'échantillonnage et l'interpolation n'ont pas été réduites.

La compilation SRML et l'installation dans le jeu portable ont réussi. La suite locale passe 60 contrôles de régression, 70 contrôles d'assembly/Harmony et 10 contrôles d'installation. La DLL portable et celle du paquet ont le même SHA256 : `4A632E767CDFB901EC6D36342D45615BF1F9289F0EBA2E5D262D5A5ACB138C1C`. Le lanceur dédié détecte le dossier portable avec `-CheckOnly`.

Ces contrôles ne remplacent pas les mesures de fluidité ni un test de session réelle. Le transfert d'autorité dans un biome éloigné, les déplacements synchronisés par lots, les drones, les gadgets, les reconnexions et les autres scénarios restent à cocher uniquement après les essais host/client de `TESTING.md`. Le self-join a une cause confirmée : serveur et client partageaient le même Product User ID EOS. Il est maintenant rejeté avec un message explicite ; un joueur ne peut rejoindre ce serveur qu'avec une identité EOS distincte.

### Itération du 25 septembre — build 1650

- **T-pose et paquets d'animation :** le serveur appliquait une file mutable en la vidant, puis relayait le même objet. Les clients distants recevaient donc des paramètres incomplets. Le relais utilise maintenant une copie indépendante. Les paquets reçus avant la création de l'Animator sont ignorés sans déréférencer les paramètres absents, et le premier état de couche envoie aussi son hash/temps initial.
- **Reconnexion EOS :** le callback `LoginStatusChanged` peut déjà avoir effacé le Product User ID quand le transport demande une nouvelle authentification. L'ancien garde quittait alors la méthode sans relancer Connect Login. Il ne bloque plus cette récupération si le Device ID est prêt. La récupération après invalidation EOS demeure à valider en vrai réseau.
- **Snapshots :** correction d'un index de slot incorrect pour les émotions de silo et lecture non signée des IDs de fashions de pièges/drones. Les nombres de programmes de drone et tailles de gadget reçus sont bornés avant allocation.
- **Désaccord de propriétaire d'acteur :** les logs montrent des suppressions refusées par l'hôte. Le refus protégeait le monde commun mais ne réparait pas la copie déjà détruite côté demandeur. L'hôte renvoie maintenant un snapshot fiable; le client applique aussi les snapshots aux acteurs déjà présents, donc leur propriétaire, leur transform et leur état modèle convergent.
- **Spawns hors de la zone hôte :** la correction d'autorité de région permet au client autoritaire d'enregistrer les acteurs de démarrage au chemin normal du GameModel. Le diagnostic `/netstate` expose maintenant les spawners et leur propriétaire pour comparer host/client.
- Build compilé avec le SDK local ; le paquet Windows est généré. `scripts/Test-SRMP.ps1` passe 154 contrôles (60/84/10). La copie portable et le payload du ZIP correspondent à la DLL construite, SHA256 `7557716D2B33903A7D9951D592310D31D151547C4219B46AD8920578038C3451`. Le SHA256 du ZIP est `9F6960A582500E6DB84895116D892FF06C8974A15E94D01E938DFEB3B07A727A`. DLL : `Builds/SRMP/SRMP.dll`; paquet : `dist/SRMP-Revival-stable-1650-Windows.zip`.

Ces corrections ont une validation statique/régression locale, pas une validation de session. Suivre les cas T-pose, reconnexion EOS et spawns de biome de `TESTING.md` avant de qualifier le résultat de confirmé en jeu.

### Correctif de la trace FPS — build 1651

- La trace fournie contient 129 répétitions du même chemin d'exception `ActorRegistry.FixedUpdate → StalkConsumable.Relevancy → FindConsumable.FindNearestConsumable → Identifiable.IsEdible → GameObject.GetComponent`. Une référence de consommable déjà détruite pouvait rester dans la recherche partagée du jeu ; chaque slime qui parcourait cette entrée provoquait une exception coûteuse et le journal était martelé.
- `Identifiable.IsEdible` reçoit maintenant un préfixe Harmony qui utilise la comparaison Unity avec `null` (qui inclut les objets natifs détruits) et retourne `false` pour cette entrée. Le comportement normal et la recherche des autres aliments continuent.
- Le même fichier montre des erreurs `PlayerAnimation`/`ActorPositions`. Les handlers actuels ont déjà les gardes d'Animator/queue et de références d'acteurs vérifiées dans l'itération 1650 ; les versions de source actuelles passent leurs tests, mais le journal fourni ne donne pas l'identité de la DLL chargée. Confirmer ces lignes avec les deux pairs sur le même build 1651.
- Build `SRML` et installation réussis. `scripts/Test-SRMP.ps1` réussit **157 contrôles (60 transport/identités/sauvegardes, 87 assembly/Harmony, 10 installateur)**. Le test ajouté valide la résolution de la cible Harmony et que l'entrée nulle est ignorée. DLL compilée, copie portable et payload du ZIP : SHA256 `95DF02A6DD4A59E7B66719BB7E094AD62C31D3D3929AE18AC6E5EFEA722A21B8`. ZIP : `dist/SRMP-Revival-stable-1651-Windows.zip`, SHA256 `A9BA6D2820724A18D8F656D6BE61797F72D1B3728A443569E64330ECE12A79BB`.
- La fluidité après correctif et l'absence de ces erreurs en session host/client restent à valider en jeu ; aucun FPS n'est déclaré mesuré.

### Despawn réseau — build 1652

- Les deux journaux (créés vers 22:13, avant l'installation 1651) montrent 14 `ActorDestroy` en échec, plus des `ActorPositions`, `ActorFX` et `ActorEmotions` appliqués à des acteurs périmés. Dans le handler, l'effet de disparition précédait la vraie suppression : une exception abandonnait le reste du handler et conservait l'entrée réseau.
- Les despawns client et serveur passent par `NetworkActor.DestroyRegistered`. Une erreur FX est journalisée une fois puis le despawn continue ; une erreur de nettoyage du jeu tente le `Destroy` Unity ; l'ID est retiré du registre dans `finally`. Les mises à jour de position vérifient l'acteur et son `RegionMember` avant de toucher aux transforms.
- Le `slimeGold(Clone)` apparaît une fois dans le même stack alimentaire `AweTowardsLargos → FindConsumable → Identifiable.IsEdible` : il n'indique pas à lui seul une règle d'IA spéciale au gold slime. Le garde introduit en 1651 couvre ce chemin.
- Les 772 erreurs `PlayerAnimation` de ces fichiers sont également antérieures à 1651 ; elles venaient de l'ancien lecteur de files. Les gardes d'initialisation et de taille sont présents dans le build actuel. Comparer les logs après un nouveau test sur un build exact partagé entre les deux pairs.
- Build 1652 compilé et installé dans la copie portable. La suite passe **160 contrôles (60/90/10)** ; `git diff --check` est propre. La DLL de build, l'installation et le payload du package ont SHA256 `4AC4E3C75E94C5C8B5627B68F14F29BBF0592D2EFACFC36840670DD442306850`. Package `dist/SRMP-Revival-stable-1652-Windows.zip`, SHA256 `9C2087E1D7ECE1B270971EAF60B2069F24F8E049D53D58A6DDECC2C3DB433F73`.
- Refaire le test gold slime/largos, les départs/destructions d'acteurs, l'animation et les frame-times en deux instances ; les fichiers actuels ne prouvent pas encore le résultat du build 1652.

Fichiers de l'itération du 25 septembre :

- `SRMP/EOS/EpicAuthentication.cs`
- `SRMP/Networking/Communication/NetworkHandlerClient.cs`
- `SRMP/Networking/Communication/NetworkHandlerServer.cs`
- `SRMP/Networking/NetworkActor.cs`
- `SRMP/Networking/NetworkNickname.cs`
- `SRMP/Networking/NetworkPlayer.Animation.cs`
- `SRMP/Networking/NetworkRegion.cs`
- `SRMP/Packets/Actors/PacketPlayerAnimation.cs`
- `SRMP/Packets/Drones/PacketDronePrograms.cs`
- `SRMP/Packets/Gadgets/PacketGadgets.cs`
- `SRMP/Packets/LandPlots/PacketLandplots.cs`
- `SRMP/Packets/_Classes/Packet.cs`
- `SRMP/Patches/Patch_GameModel.cs`
- `SRMP/Properties/AssemblyInfo.cs` (version générée par le build)
- `SRMP/modinfo.json` (version générée par le build)
- `tests/AssemblyTests.cs`
- `BUG_STATUS.md`
- `AUDIT_STABILITY.md`
- `TESTING.md`

## Fichiers modifiés ou ajoutés

Liste des fichiers de cette livraison ; `.serena/`, déjà présent avant le travail, n'a pas été modifié.

- `.gitignore`
- `AUDIT_STABILITY.md`
- `BUG_STATUS.md`
- `SRMP/Custom UI/ChatUI.cs`
- `SRMP/EOS/EpicAuthentication.cs`
- `SRMP/EOS/EpicLobby.cs`
- `SRMP/EOS/EpicNetworkClient.cs`
- `SRMP/EOS/EpicNetworkServer.cs`
- `SRMP/EOS/EpicP2P.cs`
- `SRMP/MainSRML.cs`
- `SRMP/MainStandalone.cs`
- `SRMP/Networking/Communication/NetworkHandlerClient.cs`
- `SRMP/Networking/Communication/NetworkHandlerServer.cs`
- `SRMP/Networking/NetworkActor.cs`
- `SRMP/Networking/NetworkDiagnostics.cs`
- `SRMP/Networking/NetworkDrone.cs`
- `SRMP/Networking/NetworkGadgetSite.cs`
- `SRMP/Networking/NetworkGordo.cs`
- `SRMP/Networking/NetworkIdentity.cs`
- `SRMP/Networking/NetworkNutcracker.cs`
- `SRMP/Networking/NetworkPlayer.Animation.cs`
- `SRMP/Networking/NetworkPlayer.VR.cs`
- `SRMP/Networking/NetworkPlayer.cs`
- `SRMP/Networking/NetworkRegion.cs`
- `SRMP/Networking/PacketFraming.cs`
- `SRMP/Networking/PlayerInventory.cs`
- `SRMP/Packets/Actors/PacketActorOwner.cs`
- `SRMP/Packets/Actors/PacketActors.cs`
- `SRMP/Packets/Drones/PacketDroneAmmoAdd.cs`
- `SRMP/Packets/Drones/PacketDroneAmmoRemove.cs`
- `SRMP/Packets/Gordos/PacketGordoEat.cs`
- `SRMP/Packets/ModCompat/PacketSpiritMode.cs`
- `SRMP/Packets/Players/PacketPlayerCurrency.cs`
- `SRMP/Patches/Patch_Ammo.cs`
- `SRMP/Patches/Patch_AutoSaveDirector.cs`
- `SRMP/Patches/Patch_DroneAnimator.cs`
- `SRMP/Patches/Patch_DroneStationAnimator.cs`
- `SRMP/Patches/Patch_DroneSubbehaviourRest.cs`
- `SRMP/Patches/Patch_GameModel.cs`
- `SRMP/Patches/Patch_GordoEat.cs`
- `SRMP/Patches/Patch_NutCracker.cs`
- `SRMP/Patches/Patch_PauseMenu.cs`
- `SRMP/Patches/Patch_PlayerState.cs`
- `SRMP/Patches/Patch_PlortCollector.cs`
- `SRMP/Patches/Patch_Region.cs`
- `SRMP/Patches/Patch_SlimeEat.cs`
- `SRMP/Patches/Patch_SlimeFeral.cs`
- `SRMP/Patches/Patch_Vacuumable.cs`
- `SRMP/Properties/AssemblyInfo.cs`
- `SRMP/SRMP.cs`
- `SRMP/SRMP.csproj`
- `SRMP/Utils/AtomicFile.cs`
- `SRMP/Utils/Extensions.cs`
- `SRMP/Utils/Utils.cs`
- `SRMP/modinfo.json`
- `TESTING.md`
- `installer/Install-SRMP.ps1`
- `installer/Uninstall-SRMP.ps1`
- `scripts/Build-SRMP.ps1`
- `scripts/Package-Release.ps1`
- `scripts/Test-Installer.ps1`
- `scripts/Test-SRMP.ps1`
- `tests/AssemblyTests.cs`
- `tests/Directory.Build.props`
- `tests/Program.cs`
- `tests/SRMP.AssemblyTests.csproj`
- `tests/SRMP.RegressionTests.csproj`

### Fichiers ajoutés ou modifiés dans l'itération du 23 septembre

- `SRMP/EOS/EpicApplication.cs`
- `SRMP/EOS/EpicNetworkServer.cs`
- `SRMP/Networking/NetworkIdentity.cs`
- `SRMP/Patches/Patch_PauseFix.cs`
- `SRMP/Server/AutoHost.cs`
- `SRMP/Server/AutoHostConfig.cs`
- `SRMP/Server/ServerCommands.cs`
- `SRMP/Lidgren.Network/NetBitWriter.cs`
- `SRMP/Lidgren.Network/NetBuffer.Peek.cs`
- `SRMP/Lidgren.Network/NetBuffer.Read.cs`
- `SRMP/Lidgren.Network/NetBuffer.Write.cs`
- `SRMP/Lidgren.Network/NetRandom.Implementations.cs`
- `SRMP/Lidgren.Network/NetRandom.cs`
- `SRMP/Lidgren.Network/NetRandomSeed.cs`
- `SRMP/Lidgren.Network/NetUtility.cs`
- `SRMP/Lidgren.Network/Platform/PlatformAndroid.cs`
- `SRMP/Lidgren.Network/Platform/PlatformConstrained.cs`
- `SRMP/Lidgren.Network/Platform/PlatformWin32.cs`
- `SRMP/Lidgren.Network/Platform/PlatformWinRT.cs`
- `serveur/README.md`
- `serveur/Run-Server.ps1`
- `serveur/run.cmd`
- `serveur/run.sh`
- `serveur/autohost.json`
- `serveur/srmp-server.service.example`
- `scripts/Package-Release.ps1`
- `tests/Program.cs`

Fichiers de l'itération du 24 septembre :

- `SRMP/Networking/ActorPositionBatcher.cs`
- `SRMP/Packets/Actors/PacketActorPositions.cs`
- `SRMP/Packets/_Classes/PacketType.cs`
- `SRMP/Networking/NetworkActor.cs`
- `SRMP/Networking/Communication/NetworkHandlerClient.cs`
- `SRMP/Networking/Communication/NetworkHandlerServer.cs`
- `SRMP/SRMP.cs`
- `SRMP/SRMP.csproj`
- `SRMP/Properties/AssemblyInfo.cs`
- `SRMP/modinfo.json`
- `tests/AssemblyTests.cs`
- `TESTING.md`
- `BUG_STATUS.md`
