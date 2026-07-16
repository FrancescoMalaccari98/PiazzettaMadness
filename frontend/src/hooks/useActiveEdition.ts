import { useEffect, useState } from "react";
import { api, type ApiEdition } from "../lib/api";

// Fallback mostrato solo mentre la chiamata è in corso o se il backend
// non risponde — non forza mai il 2027 sopra al dato reale del database.
const FALLBACK_EDITION: ApiEdition = {
  id: 0,
  name: "Piazzetta Madness 2027",
  year: 2027,
  status: "Draft",
  start_date: null,
  end_date: null,
  tournament: { id: 0, name: "Piazzetta Madness" },
};

export function useActiveEdition(): ApiEdition {
  const [edition, setEdition] = useState<ApiEdition>(FALLBACK_EDITION);
  useEffect(() => {
    api.getActiveEdition().then(d => { if (d) setEdition(d); });
  }, []);
  return edition;
}
