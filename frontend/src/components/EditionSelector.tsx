import type { ApiEdition } from "../lib/api";

export function EditionSelector({
  editions,
  selectedEditionId,
  onChange,
  className = "",
}: {
  editions: ApiEdition[];
  selectedEditionId: number | null;
  onChange: (editionId: number) => void;
  className?: string;
}) {
  if (editions.length === 0) return null;

  return (
    <div className={`flex flex-col sm:flex-row sm:items-center gap-3 ${className}`}>
      <label
        htmlFor="edition-select"
        className="font-display text-xs uppercase tracking-[0.25em] text-zinc-500"
      >
        Edizione
      </label>
      <select
        id="edition-select"
        value={selectedEditionId ?? ""}
        onChange={(event) => onChange(Number(event.target.value))}
        className="bg-zinc-950 border-[3px] border-zinc-800 text-white font-display uppercase tracking-widest text-sm px-4 py-3 min-w-40 outline-none hover:border-brand-orange focus:border-brand-orange"
      >
        {editions.map((edition) => (
          <option key={edition.id} value={edition.id}>
            {edition.year}
          </option>
        ))}
      </select>
    </div>
  );
}
