from __future__ import annotations

from copy import deepcopy
from typing import Any, Dict, List, Optional


def minuti_template(raw: Optional[str] = None, secondi: Optional[int] = None) -> Dict[str, Any]:
    return {"raw": raw, "secondi": secondi}


def tiro_template() -> Dict[str, Any]:
    return {"realizzati": None, "tentati": None, "percentuale": None}


def rimbalzi_template() -> Dict[str, Any]:
    return {"offensivi": None, "difensivi": None, "totali": None}


def falli_template() -> Dict[str, Any]:
    return {"commessi": None, "subiti": None}


def giocatore_template() -> Dict[str, Any]:
    return {
        "numero": None,
        "nome": None,
        "cognome": None,
        "nome_completo": None,
        "capitano": False,
        "non_entrato": False,
        "minuti": minuti_template(),
        "tiri_dal_campo": tiro_template(),
        "tiri_da_2": tiro_template(),
        "tiri_da_3": tiro_template(),
        "tiri_liberi": tiro_template(),
        "rimbalzi": rimbalzi_template(),
        "assist": None,
        "palle_perse": None,
        "palle_recuperate": None,
        "stoppate_date": None,
        "falli": falli_template(),
        "plus_minus": None,
        "valutazione": None,
        "punti": None,
    }


def squadra_template(tipo: str) -> Dict[str, Any]:
    return {
        "tipo": tipo,
        "nome": None,
        "abbreviazione": None,
        "allenatore": None,
        "vice_allenatore": None,
        "giocatori": [],
        "totali_squadra": {
            "minuti": minuti_template(),
            "tiri_dal_campo": tiro_template(),
            "tiri_da_2": tiro_template(),
            "tiri_da_3": tiro_template(),
            "tiri_liberi": tiro_template(),
            "rimbalzi": rimbalzi_template(),
            "assist": None,
            "palle_perse": None,
            "palle_recuperate": None,
            "stoppate_date": None,
            "falli": falli_template(),
            "plus_minus": None,
            "valutazione": None,
            "punti": None,
        },
    }


def comparative_template() -> Dict[str, Any]:
    return {
        "punti_da_palle_perse": {"squadra_casa": None, "squadra_ospite": None},
        "punti_in_area": {
            "squadra_casa": {"punti": None, "realizzati": None, "tentati": None, "percentuale": None},
            "squadra_ospite": {"punti": None, "realizzati": None, "tentati": None, "percentuale": None},
        },
        "punti_da_secondi_tiri": {"squadra_casa": None, "squadra_ospite": None},
        "punti_contropiede": {"squadra_casa": None, "squadra_ospite": None},
        "fast_break_points_from_turnovers": {"squadra_casa": None, "squadra_ospite": None},
        "punti_panchina": {"squadra_casa": None, "squadra_ospite": None},
        "massimo_vantaggio": {"squadra_casa": None, "squadra_ospite": None},
        "massimo_parziale": {"squadra_casa": None, "squadra_ospite": None},
        "punti_per_possesso": {"squadra_casa": None, "squadra_ospite": None},
        "cambi_guida": None,
        "parita": None,
        "tempo_in_vantaggio": {"squadra_casa": None, "squadra_ospite": None},
    }


def document_template() -> Dict[str, Any]:
    return {
        "metadata": {
            "nome_file": None,
            "fase_partita": None,
            "fonte": "FIBA Live Stats",
            "tipo_documento": "tabellino_fiba",
            "numero_pagine": 1,
        },
        "partita": {
            "competizione": None,
            "campo": None,
            "data": None,
            "ora": None,
            "numero_gara": None,
            "spettatori": None,
            "durata_gara": None,
            "report_creato": None,
            "arbitri": [],
        },
        "risultato": {
            "squadra_casa": {"nome": None, "abbreviazione": None, "punti_finali": None},
            "squadra_ospite": {"nome": None, "abbreviazione": None, "punti_finali": None},
            "punteggio_finale": None,
            "punteggio_intermedio": None,
            "periodi": [],
        },
        "squadre": [squadra_template("casa"), squadra_template("ospite")],
        "statistiche_comparative": comparative_template(),
        "affidabilita": {
            "livello_globale": "alta",
            "note": [],
            "warnings": [],
        },
    }
