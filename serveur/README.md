# Héberger SRMP à distance

Ce dossier lance l’AutoHost déjà présent dans SRMP. Il charge une sauvegarde Slime Rancher, crée le lobby EOS et affiche le code que les joueurs saisissent dans le menu multijoueur.

Le serveur reste un processus Slime Rancher/Unity en mode sans affichage : la simulation des slimes, des gadgets et des zones dépend de `GameContext`, des scènes, des assets et des coroutines du jeu. Cette version ne fournit pas un moteur de simulation autonome pouvant tourner sans les fichiers du jeu.

## Prérequis

- Une installation 64 bits de **Slime Rancher 1** avec **SRML** déjà installé.
- Une machine x64 qui reste allumée et connectée à Internet.
- Sous Windows, les fichiers du jeu et de SRML dans le même dossier.
- Sous Linux, la version Windows du jeu lancée par Proton. Le mode Linux natif n’est pas compilé ici.

Un Raspberry Pi ARM ne peut pas exécuter ce serveur : ce projet dépend du joueur Unity Windows x64 et de l’EOS SDK Windows x64. Utilise plutôt une machine x64 Windows ou Linux. L’option Linux/Proton reste à valider en situation réelle.

## Démarrer sous Windows

Extrais le paquet SRMP à côté du dossier du jeu, puis lance `run.cmd` depuis `serveur`. Le lanceur repère une copie portable située dans les dossiers voisins et démarre Unity depuis le dossier du jeu, nécessaire au chargement relatif des bibliothèques SRML.

Si plusieurs copies du jeu sont voisines, précise celle à utiliser :

```bat
run.cmd "C:\Games\Slime Rancher"
```

Le même chemin peut être fourni avec `SRMP_GAME_DIR`. Si un seul jeu se trouve près du paquet, aucun chemin n’est nécessaire.
Pour vérifier l’emplacement détecté et l’installation SRML/SRMP sans démarrer le monde, lance `run.cmd -CheckOnly`.

Le lanceur copie la DLL SRMP dans `SRML\Mods`, crée `SRMP\autohost.json` au premier démarrage, puis lance le jeu avec `-batchmode -nographics -srmp-autohost`. Après une fermeture inattendue ou un échec de démarrage, il relance le jeu après dix secondes.

## Démarrer sous Linux avec Proton

Installe Proton et `xvfb`, puis extrais le paquet près du dossier du jeu Windows. Le script détecte une copie voisine. Tu peux aussi fournir son chemin et celui de Proton :

```bash
SRMP_GAME_DIR=/srv/slime-rancher \
SRMP_PROTON="$HOME/.steam/steam/steamapps/common/Proton - Experimental/proton" \
bash serveur/run.sh
```

Le préfixe Proton persistant est créé dans `srmp-compatdata` à côté du jeu afin que l’identité EOS de l’hôte survive aux redémarrages. Si Steam est installé ailleurs, définis aussi `STEAM_COMPAT_CLIENT_INSTALL_PATH` et `STEAM_COMPAT_DATA_PATH`.

Pour un lancement au démarrage avec systemd, adapte `srmp-server.service.example`, puis active le service. Le script lui-même reprend après les sorties inattendues.

## Sauvegarde, pause et arrêt

Au premier lancement, l’AutoHost charge la sauvegarde la plus récente. Tu peux choisir un monde dans `SRMP/autohost.json` avec `GameName`. Le monde est sauvegardé quand le dernier client se déconnecte, puis le temps Unity est mis à zéro. EOS et la boucle réseau continuent de tourner en temps réel ; l’arrivée d’un joueur relance le temps du monde.

Le code d’ami est écrit dans `SRMP/servercode.txt` et affiché dans la sortie du serveur. Les joueurs s’y connectent depuis le menu SRMP habituel. `StatusIntervalSeconds` contrôle le message périodique indiquant le nombre de joueurs.

Pour arrêter proprement, crée le fichier `SRMP/shutdown.request` dans le dossier du jeu. Le serveur sauvegarde, ferme le lobby, écrit `SRMP/server.stopped` et s’arrête. Le lanceur détecte cet arrêt propre et ne redémarre pas le jeu.

## Comptes opérateurs

Les pseudos viennent du client et ne peuvent pas servir de preuve d’identité. Un joueur récupère son identifiant avec la commande de chat `/id`. Ajoute ensuite cet identifiant à `OperatorProductUserIds` dans `SRMP/autohost.json`, puis redémarre le serveur. Les anciennes entrées de pseudo dans `Operators` ne donnent aucun droit.

## Limites de validation

La compilation vérifie SRMP, mais ne remplace pas un démarrage avec Slime Rancher, SRML et deux clients. Il faut encore valider l’amorçage Unity en mode sans affichage, la connexion EOS, la pause/reprise du temps et les reprises après crash sur les machines cibles.
