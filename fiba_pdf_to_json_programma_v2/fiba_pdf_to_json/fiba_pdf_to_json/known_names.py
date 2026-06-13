from __future__ import annotations

import difflib
import re
from typing import Iterable, Optional


KNOWN_TEAMS = [
    "Saluta Andonio Spurs",
    "Atlanta Robba",
    "I Los aiche ride",
    "Minnesode Timbermilf",
    "Denver McNuggets",
    "Philadelphia 70Sexers",
    "Boston Lopez",
    "Miami Spritz",
]

KNOWN_PLAYERS = [
    # Saluta Andonio Spurs
    "Francesco Emiliani",
    "Nicolò Purifico",
    "Riccardo Morresi",
    "Giorgio Monteriù",
    "Federico Pompozzi",
    "Gianluca Leoni",
    "Fabio Ricci",
    "Simone Cimini",
    # Atlanta Robba
    "Daniele Raccosta",
    "Federico Pierleoni",
    "Alessandro Fusari",
    "Giulio Abbate",
    "Francesco Bruni",
    "Mattia Raccosta",
    "Matteo Stella",
    "Luca Cerchiara",
    # I Los aiche ride
    "Gerico Buscarini",
    "Giampaoli Riccardo",
    "Damiano Pelucchini",
    "Elia Evangelisti",
    "Edoardo Tamiozzo",
    "Massimo Morino",
    "Luca Ortenzi",
    "Tristan Buscarini",
    # Minnesode Timbermilf
    "Francesco Napoli",
    "Giulio Mori",
    "Karol Losinski",
    "Filippo Marcone",
    "Cristian Cingolani",
    "Flavius Atodiresei",
    "Giuseppe Rombini",
    "Marco Perugini",
    # Denver McNuggets
    "Marco Filo",
    "Marco Cinelli",
    "Domenico D'Attanasio",
    "Edoardo Pacioni",
    "Marco Bronzi",
    "Roberto Filo",
    "Daniele Monachesi",
    "Francesco Bindelli",
    # Philadelphia 70Sexers
    "Marco Palombini",
    "Federico Fabiani",
    "Francesco Palombini",
    "Alessandro Barbieri",
    "Lorenzo Gattafoni",
    "Ettore Merani",
    "Luca Malaspina",
    "Edoardo Digiangiacomo",
    # Boston Lopez
    "Andrea Sabatini",
    "Filippo Perugini",
    "Francesco Ripari",
    "Alberto Ortolan",
    "Michele Angeletti",
    "Alessandro Ferraro",
    "Luca Sbrancia",
    "Marco Acquaroli",
    # Miami Spritz
    "Valerio Giacobbi",
    "Giacomo Tomassini",
    "Matteo Evandri",
    "Raoul Piergentili",
    "Cristian Del Prete",
    "Giovanni Gazzani",
    "Fulvio Iannotti",
    "Riccardo Lupetti",
]


KNOWN_ROSTER: dict[str, dict[str, str]] = {
    "Saluta Andonio Spurs": {
        "Francesco Emiliani": "0",
        "Nicolò Purifico": "3",
        "Riccardo Morresi": "9",
        "Giorgio Monteriù": "6",
        "Federico Pompozzi": "1",
        "Gianluca Leoni": "7",
        "Fabio Ricci": "4",
        "Simone Cimini": "8",
    },
    "Atlanta Robba": {
        "Daniele Raccosta": "9",
        "Federico Pierleoni": "7",
        "Alessandro Fusari": "2",
        "Giulio Abbate": "0",
        "Francesco Bruni": "5",
        "Mattia Raccosta": "4",
        "Matteo Stella": "8",
        "Luca Cerchiara": "6",
    },
    "I Los aiche ride": {
        "Gerico Buscarini": "0",
        "Giampaoli Riccardo": "7",
        "Damiano Pelucchini": "9",
        "Elia Evangelisti": "3",
        "Edoardo Tamiozzo": "1",
        "Massimo Morino": "2",
        "Luca Ortenzi": "8",
        "Tristan Buscarini": "5",
    },
    "Minnesode Timbermilf": {
        "Francesco Napoli": "7",
        "Giulio Mori": "5",
        "Karol Losinski": "1",
        "Filippo Marcone": "4",
        "Cristian Cingolani": "8",
        "Flavius Atodiresei": "0",
        "Giuseppe Rombini": "9",
        "Marco Perugini": "2",
    },
    "Denver McNuggets": {
        "Marco Filo": "7",
        "Marco Cinelli": "9",
        "Domenico D'Attanasio": "0",
        "Edoardo Pacioni": "4",
        "Marco Bronzi": "5",
        "Roberto Filo": "3",
        "Daniele Monachesi": "8",
        "Francesco Bindelli": "2",
    },
    "Philadelphia 70Sexers": {
        "Marco Palombini": "4",
        "Federico Fabiani": "6",
        "Francesco Palombini": "9",
        "Alessandro Barbieri": "0",
        "Lorenzo Gattafoni": "1",
        "Ettore Merani": "5",
        "Luca Malaspina": "3",
        "Edoardo Digiangiacomo": "7",
    },
    "Boston Lopez": {
        "Andrea Sabatini": "9",
        "Filippo Perugini": "4",
        "Francesco Ripari": "6",
        "Alberto Ortolan": "1",
        "Michele Angeletti": "0",
        "Alessandro Ferraro": "7",
        "Luca Sbrancia": "5",
        "Marco Acquaroli": "3",
    },
    "Miami Spritz": {
        "Valerio Giacobbi": "4",
        "Giacomo Tomassini": "0",
        "Matteo Evandri": "8",
        "Raoul Piergentili": "5",
        "Cristian Del Prete": "7",
        "Giovanni Gazzani": "1",
        "Fulvio Iannotti": "6",
        "Riccardo Lupetti": "3",
    },
}


def roster_jersey_for(team_name: str, player_name: str) -> Optional[str]:
    """Return the expected jersey number from the roster, or None if unknown."""
    team_roster = KNOWN_ROSTER.get(team_name)
    if team_roster is None:
        team_lower = (team_name or "").lower()
        for key, value in KNOWN_ROSTER.items():
            if key.lower() == team_lower:
                team_roster = value
                break
    if team_roster is None:
        return None
    return team_roster.get(player_name)


def known_players_for_team(team_name: str) -> list[str]:
    """Return known player names for a specific team, or [] if team unknown."""
    roster = KNOWN_ROSTER.get(team_name)
    if roster is None:
        team_lower = (team_name or "").lower()
        for key, value in KNOWN_ROSTER.items():
            if key.lower() == team_lower:
                roster = value
                break
    return list(roster.keys()) if roster else []


def _norm(s: str) -> str:
    s = re.sub(r"\(\s*C\s*\)", "", s or "", flags=re.I)
    s = s.lower()
    s = re.sub(r"[^a-z0-9àèéìòù]+", " ", s)
    return re.sub(r"\s+", " ", s).strip()


def fuzzy_known_name(value: str, candidates: Iterable[str], *, cutoff: float = 0.84) -> Optional[str]:
    value_n = _norm(value)
    if not value_n:
        return None
    lookup = {_norm(c): c for c in candidates}
    best = difflib.get_close_matches(value_n, list(lookup.keys()), n=1, cutoff=cutoff)
    if best:
        return lookup[best[0]]
    return None
