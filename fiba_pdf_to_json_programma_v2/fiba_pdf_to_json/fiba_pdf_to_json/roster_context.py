"""Roster dinamico dal DB come supporto al parsing CH1.

Quando il worker riceve ``--roster-json``, questo modulo carica il roster canonico
(scritto da C# a partire da OcrMatchContext) e fornisce le stesse funzioni di
``known_names`` (lista squadre, lista giocatori, jersey atteso, fuzzy match).

Regole:
  - Il roster dinamico NON crea identità canoniche: è solo supporto al parsing.
  - Il matching canonico delle identità avviene in C# (PlayerIdentityMatcher).
  - Se nessun roster è attivo, si usa il fallback legacy ``known_names`` con un
    avviso esplicito (rimozione del fallback pianificata in Fase 9).

Formato JSON atteso (camelCase, prodotto da C#):
    {
      "matchId": 42,
      "homeTeam": {"teamId": 5, "name": "Virtus",
                   "players": [{"playerId": 101, "teamId": 5,
                                "firstName": "Mario", "lastName": "Rossi",
                                "jerseyNumber": 3}]},
      "awayTeam": {...}
    }
"""
from __future__ import annotations

import json
import logging
from typing import Iterable, Optional

from . import known_names as _legacy

LOGGER = logging.getLogger(__name__)

_active: "Optional[RosterContext]" = None
_warned_legacy = False


class RosterContext:
    """Roster di una singola partita (entrambe le squadre)."""

    def __init__(self, teams: list[str], players: list[str], roster: dict[str, dict[str, str]]):
        self.teams = teams
        self.players = players
        self.roster = roster  # {team_name: {player_full_name: jersey_str}}

    @staticmethod
    def from_json(data: dict) -> "RosterContext":
        teams: list[str] = []
        players: list[str] = []
        roster: dict[str, dict[str, str]] = {}
        for key in ("homeTeam", "awayTeam"):
            team = data.get(key) or {}
            name = (team.get("name") or "").strip()
            if not name:
                continue
            teams.append(name)
            team_map: dict[str, str] = {}
            for player in team.get("players") or []:
                first = (player.get("firstName") or "").strip()
                last = (player.get("lastName") or "").strip()
                full = f"{first} {last}".strip()
                if not full:
                    continue
                jersey = player.get("jerseyNumber")
                team_map[full] = "" if jersey is None else str(jersey)
                players.append(full)
            roster[name] = team_map
        return RosterContext(teams, players, roster)

    def _team_map(self, team_name: str) -> Optional[dict[str, str]]:
        team_map = self.roster.get(team_name)
        if team_map is None:
            team_lower = (team_name or "").lower()
            for key, value in self.roster.items():
                if key.lower() == team_lower:
                    return value
        return team_map

    def roster_jersey_for(self, team_name: str, player_name: str) -> Optional[str]:
        team_map = self._team_map(team_name)
        return team_map.get(player_name) if team_map else None

    def known_players_for_team(self, team_name: str) -> list[str]:
        team_map = self._team_map(team_name)
        return list(team_map.keys()) if team_map else []


def load_and_activate(path: str) -> bool:
    """Carica il roster dal file JSON e lo rende attivo. Ritorna True se attivato."""
    global _active
    try:
        with open(path, "r", encoding="utf-8") as handle:
            data = json.load(handle)
        context = RosterContext.from_json(data)
        if not context.teams or not context.players:
            LOGGER.warning("Roster JSON vuoto o incompleto (%s): uso fallback known_names.", path)
            _active = None
            return False
        _active = context
        LOGGER.info("Roster dinamico attivo: %d squadre, %d giocatori.", len(context.teams), len(context.players))
        return True
    except Exception as exc:  # il roster è un supporto: non far fallire l'OCR
        LOGGER.warning("Impossibile caricare il roster JSON (%s): %s. Uso fallback known_names.", path, exc)
        _active = None
        return False


def clear_active_roster() -> None:
    global _active
    _active = None


def has_active_roster() -> bool:
    return _active is not None


def _warn_legacy_once() -> None:
    global _warned_legacy
    if not _warned_legacy:
        LOGGER.warning(
            "Roster dinamico non attivo: uso known_names.py (legacy, rimozione pianificata in Fase 9)."
        )
        _warned_legacy = True


def known_teams() -> list[str]:
    if _active is not None:
        return _active.teams
    _warn_legacy_once()
    return _legacy.KNOWN_TEAMS


def known_players() -> list[str]:
    if _active is not None:
        return _active.players
    _warn_legacy_once()
    return _legacy.KNOWN_PLAYERS


def roster_jersey_for(team_name: str, player_name: str) -> Optional[str]:
    if _active is not None:
        return _active.roster_jersey_for(team_name, player_name)
    return _legacy.roster_jersey_for(team_name, player_name)


def known_players_for_team(team_name: str) -> list[str]:
    if _active is not None:
        return _active.known_players_for_team(team_name)
    return _legacy.known_players_for_team(team_name)


def fuzzy_known_name(value: str, candidates: Iterable[str], *, cutoff: float = 0.84) -> Optional[str]:
    """Fuzzy match generico: stessa implementazione per roster dinamico e legacy.

    Le ``candidates`` provengono già da ``known_teams()`` / ``known_players()`` /
    ``known_players_for_team()``, che selezionano la sorgente corretta.
    """
    return _legacy.fuzzy_known_name(value, candidates, cutoff=cutoff)
