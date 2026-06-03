import React, { useState, useEffect } from "react";
import { motion, AnimatePresence } from "motion/react";
import { X, ChevronLeft, ChevronRight } from "lucide-react";

const API = import.meta.env.VITE_API_URL ?? "";

const defaultPhotos = [
  "https://images.unsplash.com/photo-1546519638-68e109498ffc?auto=format&fit=crop&q=80&w=1200",
  "https://images.unsplash.com/photo-1515523110800-9415d13b84a8?auto=format&fit=crop&q=80&w=1200",
  "https://images.unsplash.com/photo-1518481612222-68bbe828def1?auto=format&fit=crop&q=80&w=1200",
  "https://images.unsplash.com/photo-1504450758481-7338eba7524a?auto=format&fit=crop&q=80&w=1200",
  "https://images.unsplash.com/photo-1627627256672-027a05fffce0?auto=format&fit=crop&q=80&w=1200",
  "https://images.unsplash.com/photo-1579952363873-27f3bade9f55?auto=format&fit=crop&q=80&w=1200",
  "https://images.unsplash.com/photo-1526627702844-934336aa6b60?auto=format&fit=crop&q=80&w=1200",
  "https://images.unsplash.com/photo-1560023907-5f339617ea2d?auto=format&fit=crop&q=80&w=1200",
];

export function Photos() {
  const [photos, setPhotos] = useState<string[]>(defaultPhotos);
  const [selectedPhotoIndex, setSelectedPhotoIndex] = useState<number | null>(null);

  useEffect(() => {
    fetch(`${API}/api/foto`)
      .then(r => r.ok ? r.json() as Promise<string[]> : null)
      .then(data => { if (data) setPhotos(data); })
      .catch(() => {});
  }, []);

  const handleNext = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (selectedPhotoIndex !== null) {
      setSelectedPhotoIndex((selectedPhotoIndex + 1) % photos.length);
    }
  };

  const handlePrev = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (selectedPhotoIndex !== null) {
      setSelectedPhotoIndex((selectedPhotoIndex - 1 + photos.length) % photos.length);
    }
  };

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (selectedPhotoIndex === null) return;
      if (e.key === "ArrowRight") setSelectedPhotoIndex((selectedPhotoIndex + 1) % photos.length);
      if (e.key === "ArrowLeft") setSelectedPhotoIndex((selectedPhotoIndex - 1 + photos.length) % photos.length);
      if (e.key === "Escape") setSelectedPhotoIndex(null);
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [selectedPhotoIndex, photos]);

  return (
    <div className="pt-32 pb-20 px-4 sm:px-6 lg:px-8">
      <div className="max-w-7xl mx-auto">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="text-center mb-12"
        >
          <h1 className="font-display text-[80px] md:text-[120px] text-brand-orange uppercase leading-[0.8] mb-8 tracking-[-4px]">
            Hall of Fame
          </h1>
          <p className="text-xl font-sans text-zinc-400">
            I momenti più epici della Madness. Sudore, sangue e highlights.
          </p>
        </motion.div>

        <div className="columns-1 sm:columns-2 lg:columns-3 gap-6 space-y-6">
          {photos.map((url, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, scale: 0.9 }}
              whileInView={{ opacity: 1, scale: 1 }}
              viewport={{ once: true, margin: "-50px" }}
              transition={{ delay: (i % 3) * 0.1, duration: 0.4 }}
              className="break-inside-avoid relative group overflow-hidden border-[3px] border-zinc-800 hover:border-brand-blue transition-colors cursor-pointer"
              onClick={() => setSelectedPhotoIndex(i)}
            >
              <div className="absolute inset-0 bg-brand-blue/20 opacity-0 group-hover:opacity-100 transition-opacity z-10 pointer-events-none mix-blend-overlay"></div>
              {/* loading="lazy": le foto della griglia sono sotto la fold, vengono
                  scaricate solo quando l'utente ci scrolla sopra */}
              <img
                src={url}
                alt={`Madness action moment ${i + 1}`}
                className="w-full h-auto object-cover grayscale opacity-80 group-hover:grayscale-0 group-hover:opacity-100 transition-all duration-500 scale-100 group-hover:scale-105"
                loading="lazy"
                referrerPolicy="no-referrer"
              />
            </motion.div>
          ))}
        </div>
      </div>

      {/* Lightbox Modal */}
      <AnimatePresence>
        {selectedPhotoIndex !== null && (
          <div className="fixed inset-0 z-[100] flex items-center justify-center">
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              onClick={() => setSelectedPhotoIndex(null)}
              className="absolute inset-0 bg-black/95 backdrop-blur-md cursor-pointer"
            ></motion.div>

            <motion.div
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.9 }}
              className="relative z-10 w-full max-w-5xl px-4 flex flex-col items-center select-none"
            >
              <button
                onClick={() => setSelectedPhotoIndex(null)}
                className="absolute -top-12 right-4 md:right-0 text-zinc-400 hover:text-brand-orange transition-colors"
              >
                <X size={36} />
              </button>

              <div className="relative w-full aspect-video md:aspect-[16/9] flex items-center justify-center border-[4px] border-brand-blue shadow-[16px_16px_0_var(--color-brand-orange)] bg-zinc-950">
                <img
                  src={photos[selectedPhotoIndex]}
                  alt={`Madness expanded ${selectedPhotoIndex + 1}`}
                  className="max-h-full max-w-full object-contain"
                  referrerPolicy="no-referrer"
                />

                <button
                  onClick={handlePrev}
                  className="absolute left-2 md:-left-16 top-1/2 -translate-y-1/2 p-2 bg-brand-blue/80 hover:bg-brand-orange text-white transition-colors border-[2px] border-transparent backdrop-blur-sm"
                >
                  <ChevronLeft size={32} />
                </button>
                <button
                  onClick={handleNext}
                  className="absolute right-2 md:-right-16 top-1/2 -translate-y-1/2 p-2 bg-brand-blue/80 hover:bg-brand-orange text-white transition-colors border-[2px] border-transparent backdrop-blur-sm"
                >
                  <ChevronRight size={32} />
                </button>
              </div>
              
              <div className="mt-8 font-mono text-brand-yellow text-xl bg-zinc-900 border border-zinc-800 px-4 py-1">
                {selectedPhotoIndex + 1} <span className="text-zinc-600">/</span> {photos.length}
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
}
