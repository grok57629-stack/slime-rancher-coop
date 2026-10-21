# SRMP Revival — two-client regression checklist

Use two legitimate Slime Rancher 1 x64 installations/users with the same SRMP and SRML versions. Prefer a fresh test save first, then repeat the important cases on an existing save.

## Baseline

- [ ] Both clients launch with SRML and SRMP without exceptions.
- [ ] Multiplayer UI appears at 1920×1080 and at a smaller resolution such as 1366×768.
- [ ] Host creates a session; client joins; both can move and see each other.
- [ ] Save, exit, relaunch and reconnect.

## Fresh-install / initialization

- [ ] On a genuinely fresh SRMP install, do **not** open Manage DLCs before testing.
- [ ] Slimes eat normally on first launch; no restart workaround is required.
- [ ] DLC entitlement/state is correct immediately.
- [ ] Client leaves and rejoins; DLC state remains correct.

## Ranch replication

- [ ] Put several plorts/items in collector range; collector pulls the correct objects for host and client.
- [ ] Run the collector repeatedly while both users interact with the corral.
- [ ] Nutcracker emits the exact expected count and object type on both clients.
- [ ] Feed several slime species and largos; remote emotion state is consistent.
- [ ] Verify normal plort production and favorite-food double-plort behavior.
- [ ] Pop at least two Gordo types; all expected rewards appear for host and client.

## Gadgets / drones / performance

- [ ] Place and remove gadgets repeatedly while watching frame-time, not only average FPS.
- [ ] In a slime-dense area, run build 1651 with host/client and watch frame-time plus logs for at least five minutes; slimes should continue eating and there should be no repeated `StalkConsumable.Relevancy → Identifiable.IsEdible → GetComponent` exceptions. Compare the same save/area after a restart if the hitch remains.
- [ ] Place/program/refill drones from host and client where supported.
- [ ] Let drones run for at least 20–30 minutes across cell transitions.
- [ ] Sleep/fast-forward time and verify drones recover normally.
- [ ] Leave/rejoin while drones are active and confirm they resume correctly.

## Chat / player state

- [ ] Host → client chat: at least 20 messages including short, long, punctuation and Unicode text.
- [ ] Client → host chat: same matrix; no empty messages and no delayed chat re-open after send.
- [ ] Apply each relevant player upgrade on host and verify remote state.
- [ ] Apply upgrades with client connected, then reconnect and verify persistence.

## Stress / save integrity

- [ ] Two players exchange rewards at nearly the same time.
- [ ] Enter/leave ranch cells repeatedly.
- [ ] Save during an active session, exit normally, reload and reconnect.
- [ ] Verify no duplicated/lost inventory caused by reconnect.
- [ ] Check SRML/SRMP logs after every failure and preserve the first exception plus preceding network events.

## Dedicated headless server

- [ ] On x64 Windows with Slime Rancher 1 and SRML installed, start `serveur/run.cmd`; verify the latest save loads and the server code appears in `SRMP/servercode.txt` and the console.
- [ ] Join from a separate game installation, confirm world time advances, then disconnect every client. The host log should report the idle pause and an immediate save; compare the in-game day/time before and after a 10-minute idle interval.
- [ ] Rejoin while the server is idle. Verify the same save resumes, world time advances again, and EOS/P2P remains responsive while paused.
- [ ] Create `SRMP/shutdown.request`. Verify save, lobby close, `SRMP/server.stopped`, and that the wrapper exits without relaunching the game.
- [ ] Remove the clean-stop marker, relaunch, then terminate the game process unexpectedly. Verify the wrapper restarts it and publishes a usable server code again.
- [ ] Configure an operator by EOS Product User ID from `/id`. Attempt privileged commands from that account, then use the same display name on a different account; only the configured EOS identity should retain operator rights.
- [ ] Repeat the start/join/idle/rejoin/stop sequence on Linux with Proton. Record the Proton version and prefix path; this path is experimental and has not been validated by this audit.

One isolated headless smoke test was run on build 1637 under Windows: SRML and SRMP loaded and EOS Device ID login succeeded. The test selected a nonexistent sentinel save with `CreateGameIfMissing=false`, so it exited before opening a world or lobby and did not touch an existing save. It does not validate the checklist above. The Unity log is `artifacts/headless-smoke-1637-20260923.log`; the log also contains a native `AUTH_PASSWORD` diagnostic before the successful Device ID login.

For full runs, preserve the AutoHost console, Unity log, `SRMP/Logs` diagnostics and `servercode.txt`. Keep every checkbox clear until that specific case has been executed on a live host and client.

## Pass rule

Do not call the revival “all known bugs fixed” until every historical item has a reproducible pass on at least two consecutive clean runs. Intermittent defects should get a GitHub issue with logs and exact reproduction steps.


## Audit du 21 septembre 2026 — procédure complémentaire

### Tests locaux

Après compilation : `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-SRMP.ps1`.
Passer `-GamePath` / `-DotNetPath` si nécessaire. Ces tests ne lancent pas Slime Rancher.

### Captures comparables

Sur chaque instance, saisir `/netstate` dans le chat. Conserver le fichier récent sous `<jeu>/SRMP/Logs/` : les blocs `[NetState] BEGIN` / `END` contiennent rôle, build, ID local, readiness, monnaie, régions/propriétaires, tuples d'acteurs `id:owner:type:region`, compteurs Gordo et inventaires drone. Les joints de collecteur doivent exister seulement sur son autorité.

Capturer avant/après chaque scénario en laissant les objets s'immobiliser. Comparer les acteurs dans la même zone : le client ne connaît pas forcément les acteurs d'une région jamais visitée.

| Scénario | Reproduction | Résultat attendu / comparaison |
|---|---|---|
| Réseau | Simuler latence/perte avec un outil réseau adapté ; déplacer puis immobiliser un objet et générer un gros snapshot. | Créations/destructions reçues, pas de doublon ni erreur de parsing ; dernière pose rattrapée. |
| Join tardif | Créer slimes/plorts/gadgets et lancer un drone ; connecter deux clients dont les chargements se chevauchent. | Un avatar par joueur, monde présent avant readiness, inventaire chargé une fois. |
| Reconnexion | Quitter/rejoindre dix fois ; couper le réseau pendant l'authentification puis en session ; relancer l'hôte. | Aucun lobby bloqué, joueur fantôme ou propriétaire absent ; inventaire/pose restaurés. |
| DLC | Démarrer sans visiter Manage DLCs ; joindre avec ensembles identiques puis différents ; reconnecter. | Ensembles identiques acceptés, différence expliquée dans les logs `[Join] ... DLCs`. |
| Gordos | Hôte dans une autre zone, deux clients nourrissent le même Gordo jusqu'au seuil ; join après éclatement ; refaire avec un piège. | Même compteur absolu, nourriture retirée partout, une seule série de récompenses, count=-1. Comparer `[Gordo]` / `[NetState]`. |
| Repas/transfert | Nourriture et slime de propriétaires différents ; prendre/reposer pendant l'animation. | Aucun repas multiple, acteur disparu localement ou famine persistante ; mêmes plorts par ID/quantité. |
| Collecteur | Plort directement à l'entrée, activation par non-propriétaire, transfert de région pendant collecte. | Propriété confirmée avant stockage/destruction, anciens joints libérés, quantités conservées. |
| Drones | Programmer/remplir de chaque côté, retirer plusieurs unités, remplacer le gadget, dormir, changer de zone, reconnecter. | Inventaires égaux, station animée, reprise sans duplication ni exception ; observer au moins 30 minutes. |
| Feral | Comparer peur/faim/agitation/aura, nourrir, transférer l'autorité puis attendre l'expiration. | Pas d'aura doublée ni destruction locale ; relever tout désaccord de temporisateur. |
| Upgrades | Débloquer/acheter avec client connecté puis pendant son chargement ; reconnecter. | Listes acquises/disponibles, capacités et popups cohérents ; aucune erreur Harmony au lancement. |
| Argent | Ventes/achats simultanés par deux clients, y compris avec solde presque épuisé. | Totaux finaux égaux ; relever le double achat concurrent, transaction complète encore ouverte. |
| Chat | Unicode, texte long/vide, Entrée maintenue, Escape, `/help` hôte, quitter/rejoindre. | Une émission, aucune réouverture différée ni texte résiduel, réponses locales aux commandes. |
| Nutcracker | Casser depuis chaque propriétaire, quitter la zone avant la fin, compter types/quantités/IDs des sorties. | Coroutine valide, pas de référence détruite, sorties conformes au jeu. |
| Profil/sauvegarde | Profil neuf, JSON tronqué et backup valide, inventaire corrompu, deux hôtes avec monde homonyme. | Pas d'application partielle, original conservé/log explicite, inventaires séparés après migration. |

### Vérifications ajoutées le 24 septembre 2026

| Scénario | Reproduction | Résultat attendu |
|---|---|---|
| Auto-connexion du serveur | Lancer le serveur dédié et le jeu avec le même profil EOS, puis saisir son propre code à 7 caractères. | Le client affiche qu'il s'agit de la même identité EOS et revient au menu sans envoyer de paquet d'authentification au serveur ni déconnecter l'ami déjà présent. |
| Autorité hors biome | L'hôte reste au Ranch ; un client entre dans un biome où aucun `NetworkRegion` n'est chargé côté hôte. Comparer `/netstate`, puis attendre les vagues de spawn. | Le serveur enregistre la présence du client, lui accorde l'autorité de cette région et les slimes/plorts apparaissent chez chaque joueur. Aucun propriétaire absent ne reste bloqué. |
| Transfert d'autorité de biome | Deux clients entrent dans le même biome éloigné de l'hôte ; celui qui possède la région quitte ou perd sa connexion. | La propriété passe au client toujours présent ; spawners et acteurs continuent de se synchroniser. Si personne ne reste, la région redevient disponible. |
| Charge du serveur inactif | Mesurer FPS/CPU serveur sans joueur, puis joindre un client et mesurer à nouveau. | Au repos, le monde est en pause et le tick passe à 12 FPS par défaut ; à l'arrivée d'un joueur, le tick revient à 60 FPS. Vérifier que l'EOS reste joignable. |
| Charge de réplication | Dans un biome chargé, déplacer plusieurs centaines de slimes/objets mobiles et comparer le nombre de paquets de position avec un build précédent. | Les positions restent fluides et autorisées, mais sont regroupées par lots de 20 maximum ; aucune charge longue ne s'accumule et les mesures de frame-time s'améliorent ou restent stables. |
| Noms optionnels | Avec le mod Nicknames actif, joindre lorsqu'une région contient des slimes et Gordos sans composant de surnom. | Le snapshot se termine sans exception ; les noms présents se synchronisent et les autres restent vides. |
| Spawner dynamique | Faire apparaître un spawner après le chargement de la région, puis changer de zone et transférer l'autorité. | Le patch crée son binding réseau depuis la région parente ; un spawner sans région émet un seul diagnostic et ne crée pas de slimes en double. |
| Erreur d'un paquet | Pendant les essais, conserver le log en cas de paquet impossible à appliquer. | Une erreur locale indique le type de paquet et l'ID du joueur/client ; elle n'est plus silencieuse. |

Pour chaque essai, noter build/SHA256, versions jeu/SRML, DLC/mods, rôles, sauvegarde, étapes exactes, première exception et captures avant/après. Cocher un cas uniquement après exécution réelle.

### Vérifications ajoutées le 25 septembre 2026 — build 1650

| Scénario | Reproduction | Résultat attendu |
|---|---|---|
| Avatar T-pose | Avec deux joueurs, faire rejoindre le client avant/pendant la création de l'avatar; marcher, sauter, vacuumer, utiliser un gadget et changer de zone. Refaire après reconnexion. | Aucun T-pose permanent; les animations initiales et les paramètres restent visibles. Aucune erreur `Queue.Empty`, `parameters` nul ou `ActorPositions` sur objet détruit. |
| Récupération identité EOS | Rejoindre une session, provoquer une perte réseau, fermer/réouvrir le jeu puis reconnecter; conserver les logs EOS/P2P des deux pairs. | Une seule tentative de login à la fois; après `EOS_InvalidUser`, la session se ferme proprement, l'identité est réauthentifiée, puis un nouveau lobby peut être rejoint sans redémarrage forcé. |
| Silo/fashions | Synchroniser un silo avec un slot vide avant un slot qui porte des émotions; transporter une fashion dont l'ID est supérieur à 32767 sur piège et drone. | Même slot/émotion et même fashion sur host/client; aucune exception de désérialisation. |
| Entrée en biome sans host | Hôte dans une autre zone, un client entre seul dans le biome. Sauvegarder `/netstate` des deux joueurs au moment de l'entrée, après deux vagues de spawn et après reconnexion. | Le propriétaire de région est un joueur connecté au biome; les acteurs/spawners ne sont présents qu'une fois puis convergent sur chaque pair. |
| Paquet drone incomplet | Dans un environnement de test, fournir à `PacketDronePrograms` un count négatif, démesuré ou un payload tronqué. | Refus du paquet borné et journalisé; aucune allocation massive/crash du client. |
| Réconciliation d'autorité | Sur le client, simuler un état propriétaire périmé puis laisser l'hôte rejeter une demande de destruction; répéter avec l'acteur absent localement et avec l'acteur toujours présent. | Le snapshot fiable recrée l'acteur absent ou corrige le propriétaire/état du présent. Plus de demande de destruction répétée pour le même acteur; comparer les IDs et owners dans `/netstate`. |

### Vérifications ajoutées le 25 septembre 2026 — build 1651

| Scénario | Reproduction | Résultat attendu |
|---|---|---|
| Références de nourriture détruites / hitch FPS | Charger un biome dense en slimes, supprimer/consommer rapidement plusieurs objets alimentaires et observer le journal et les frame-times pendant au moins cinq minutes, côté hôte et client. | Aucun martèlement de `NullReferenceException` depuis `Identifiable.IsEdible`; les slimes continuent à chercher/manger les aliments restants et les frame-times ne connaissent plus le hitch associé aux exceptions. Le patch a un test automatisé pour l'entrée nulle ; la validation Unity réelle reste à faire. |
| Erreurs résiduelles d'animation/acteur | Avec les deux pairs sur build 1651 exact, rejoindre tôt pendant le chargement de l'avatar, déplacer/changer de biome, puis quitter et rejoindre. | Aucun `Queue.Empty` depuis `ReadParameters` ni `NullReferenceException` depuis `PositionRotationUpdate`; relever version/hash de DLL et les premiers logs côté host/client si cela réapparaît. |

### Vérifications ajoutées le 25 septembre 2026 — build 1652

| Scénario | Reproduction | Résultat attendu |
|---|---|---|
| Gold slime / largos | Sur build 1652 des deux côtés, garder un slime doré et des largos dans un biome actif pendant qu'ils approchent/disparaissent ; surveiller `AweTowardsLargos` et les frame-times. | Aucun `NullReferenceException` via `FindConsumable` / `Identifiable.IsEdible`; le gold slime reste jouable, les autres slimes cherchent toujours les aliments valides. |
| Despawn FX invalide | Faire détruire plusieurs slimes/plorts depuis chaque propriétaire, dont un acteur qui disparaît pendant des packets FX/position. | Le client et le serveur retirent l'acteur une seule fois ; une FX manquante n'empêche pas le despawn ; aucun ID fantôme dans `/netstate` et pas d'erreur répétée `ActorDestroy`. |
| Animations et acteurs après reconnexion | Avec build 1652 exact, répéter join/leave/rejoin pendant le déplacement et la disparition de slimes. | Plus de `Queue.Empty` / `ActorPositions` / `ActorDestroy` en boucle ; l'avatar et les acteurs convergent après reconnexion. Les fichiers fournis sont antérieurs aux builds 1651/1652 et ne valent pas validation de ceux-ci. |
