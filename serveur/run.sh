#!/usr/bin/env bash
set -euo pipefail

server_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$server_dir/.." && pwd)"
game_dir="${SRMP_GAME_DIR:-${1:-}}"

is_game_install() {
  [[ -f "$1/SlimeRancher.exe" && -d "$1/SlimeRancher_Data/Managed" ]]
}

if [[ -n "$game_dir" ]]; then
  if ! is_game_install "$game_dir"; then
    echo "The supplied path is not a Slime Rancher 1 install: $game_dir" >&2
    exit 2
  fi
else
  nearby_candidates=()
  add_candidate() {
    local candidate="$1"
    local existing
    if ! is_game_install "$candidate"; then return; fi
    candidate="$(cd -- "$candidate" && pwd)"
    for existing in "${nearby_candidates[@]}"; do
      [[ "$existing" == "$candidate" ]] && return
    done
    nearby_candidates+=("$candidate")
  }

  for start in "$server_dir" "$(pwd)"; do
    probe="$(cd -- "$start" && pwd)"
    depth=0
    while [[ "$depth" -le 4 ]]; do
      add_candidate "$probe"
      while IFS= read -r -d '' child; do add_candidate "$child"; done \
        < <(find "$probe" -mindepth 1 -maxdepth 1 -type d -print0 2>/dev/null)
      parent="$(dirname -- "$probe")"
      [[ "$parent" == "$probe" ]] && break
      probe="$parent"
      depth=$((depth + 1))
    done
  done

  if [[ "${#nearby_candidates[@]}" -eq 1 ]]; then
    game_dir="${nearby_candidates[0]}"
    echo "Found portable Slime Rancher beside the server package: $game_dir"
  elif [[ "${#nearby_candidates[@]}" -gt 1 ]]; then
    srml_candidates=()
    for candidate in "${nearby_candidates[@]}"; do
      [[ -d "$candidate/SRML/Mods" ]] && srml_candidates+=("$candidate")
    done
    if [[ "${#srml_candidates[@]}" -eq 1 ]]; then
      game_dir="${srml_candidates[0]}"
      echo "Found portable Slime Rancher with SRML beside the server package: $game_dir"
    else
      printf 'More than one nearby Slime Rancher install was found. Set SRMP_GAME_DIR or pass the game folder:\n' >&2
      printf '  %s\n' "${nearby_candidates[@]}" >&2
      exit 2
    fi
  else
    echo 'Could not find Slime Rancher beside this package. Set SRMP_GAME_DIR or pass the game folder.' >&2
    echo 'Example: SRMP_GAME_DIR=/srv/slime-rancher ./serveur/run.sh' >&2
    exit 2
  fi
fi
game_dir="$(cd -- "$game_dir" && pwd)"

mods_dir="$game_dir/SRML/Mods"
if [[ ! -d "$mods_dir" ]]; then
  echo "SRML is missing; install SRML first so this folder exists: $mods_dir" >&2
  exit 2
fi

source_dll=''
for candidate in "$server_dir/../payload/SRMP.dll" "$repo_root/Builds/SRMP/SRMP.dll"; do
  if [[ -f "$candidate" ]]; then source_dll="$candidate"; break; fi
done
if [[ -z "$source_dll" ]]; then
  echo 'SRMP.dll is missing. Extract the SRMP release package or build the project first.' >&2
  exit 2
fi

if ! cmp -s "$source_dll" "$mods_dir/SRMP.dll" 2>/dev/null; then
  install -m 0644 "$source_dll" "$mods_dir/SRMP.dll"
  echo "Installed SRMP.dll in $mods_dir"
fi

data_dir="$game_dir/SRMP"
logs_dir="$data_dir/Logs"
mkdir -p "$logs_dir"
if [[ ! -f "$data_dir/autohost.json" ]]; then
  cp "$server_dir/autohost.json" "$data_dir/autohost.json"
  echo "Created first-run server settings: $data_dir/autohost.json"
fi

proton="${SRMP_PROTON:-${PROTON:-}}"
if [[ -z "$proton" ]]; then
  proton="$(command -v proton || true)"
fi
if [[ -z "$proton" || ! -f "$proton" ]]; then
  echo 'Set SRMP_PROTON to the Proton executable from Steam (or put proton on PATH).' >&2
  exit 2
fi

export STEAM_COMPAT_DATA_PATH="${STEAM_COMPAT_DATA_PATH:-$game_dir/srmp-compatdata}"
export STEAM_COMPAT_CLIENT_INSTALL_PATH="${STEAM_COMPAT_CLIENT_INSTALL_PATH:-${HOME}/.steam/steam}"
mkdir -p "$STEAM_COMPAT_DATA_PATH"

if [[ -z "${DISPLAY:-}" ]] && command -v xvfb-run >/dev/null 2>&1; then
  launch_prefix=(xvfb-run -a "$proton" run)
else
  launch_prefix=("$proton" run)
fi

stopped_marker="$data_dir/server.stopped"
rm -f "$stopped_marker"
echo "Starting the Windows game under Proton from $game_dir"
echo 'This is a headless game host, not a native Linux server binary.'
echo "For a clean stop, create: $data_dir/shutdown.request"

cd -- "$game_dir"
while [[ ! -f "$stopped_marker" ]]; do
  log_path="$logs_dir/Unity-$(date -u +%Y%m%d-%H%M%S).log"
  windows_log_path="Z:${log_path//\//\\}"
  echo "Launching Slime Rancher; log: $log_path"
  status=0
  "${launch_prefix[@]}" "$game_dir/SlimeRancher.exe" \
    -batchmode -nographics -srmp-autohost -logFile "$windows_log_path" || status=$?

  [[ -f "$stopped_marker" ]] && break
  echo "Slime Rancher exited with code $status; restarting in 10 seconds." >&2
  sleep 10
done

echo 'SRMP server stopped cleanly.'
