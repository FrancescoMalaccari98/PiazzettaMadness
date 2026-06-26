"""Roster dinamico dal DB come supporto al parsing CH1.

Quando il worker riceve ``--roster-json``, questo modulo carica il roster canonico
(scritto da C# a partire da OcrMatchContext) e fornisce le funzioni di supporto al
parser (lista squadre, lista giocatori, jersey atteso, fuzzy match).

Regole (dopo Fase 9 — rimozione di ``known_names.py``):
  - Il DB è l'unica fonte di identità: NON esiste più un roster hardcoded di fallback.
  - Il roster dinamico NON crea identità canoniche: è solo supporto al parsing.
  - Il matching canonico delle identità avviene in C# (PlayerIdentityMatcher).
  - Se nessun roster è attivo, le funzioni restituiscono liste vuote / ``None``: il
    parser procede senza correzione di nomi/numeri (le identità si risolvono in C#).

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

import difflib
import json
import logging
import re
from typing import Iterable, Optional

LOGGER = logging.getLogger(__name__)

_active: "Optional[RosterContext]" = None


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
    """Carica il roster dal file JSON e lo rende attivo. Ritorna True se attivato.

    Nessun fallback hardcoded: se il file è assente/vuoto/invalido, il roster resta
    inattivo e il parser procede senza correzione nomi/numeri.
    """
    global _active
    try:
        with open(path, "r", encoding="utf-8") as handle:
            data = json.load(handle)
        context = RosterContext.from_json(data)
        if not context.teams or not context.players:
            LOGGER.warning("Roster JSON vuoto o incompleto (%s): roster dinamico non attivo.", path)
            _active = None
            return False
        _active = context
        LOGGER.info("Roster dinamico attivo: %d squadre, %d giocatori.", len(context.teams), len(context.players))
        return True
    except Exception as exc:  # il roster è un supporto: non far fallire l'OCR
        LOGGER.warning("Impossibile caricare il roster JSON (%s): %s. Roster dinamico non attivo.", path, exc)
        _active = None
        return False


def clear_active_roster() -> None:
    global _active
    _active = None


def has_active_roster() -> bool:
    return _active is not None


def known_teams() -> list[str]:
    return _active.teams if _active is not None else []


def known_players() -> list[str]:
    return _active.players if _active is not None else []


def roster_jersey_for(team_name: str, player_name: str) -> Optional[str]:
    return _active.roster_jersey_for(team_name, player_name) if _active is not None else None


def known_players_for_team(team_name: str) -> list[str]:
    return _active.known_players_for_team(team_name) if _active is not None else []


def _norm(s: str) -> str:
    s = re.sub(r"\(\s*C\s*\)", "", s or "", flags=re.I)
    s = s.lower()
    s = re.sub(r"[^a-z0-9àèéìòù]+", " ", s)
    return re.sub(r"\s+", " ", s).strip()


def fuzzy_known_name(value: str, candidates: Iterable[str], *, cutoff: float = 0.84) -> Optional[str]:
    """Fuzzy match generico: trova il candidato più vicino a ``value``, o None.

    Le ``candidates`` provengono da ``known_teams()`` / ``known_players()`` /
    ``known_players_for_team()`` (roster dinamico attivo) — vuote se nessun roster
    è attivo, nel qual caso ritorna sempre None.
    """
    value_n = _norm(value)
    if not value_n:
        return None
    lookup = {_norm(c): c for c in candidates}
    best = difflib.get_close_matches(value_n, list(lookup.keys()), n=1, cutoff=cutoff)
    if best:
        return lookup[best[0]]
    return None
