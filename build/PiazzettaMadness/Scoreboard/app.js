const state = {
  homeName: "A2",
  awayName: "A4",
  homeColor: "#f77f00",
  awayColor: "#457b9d",
  homeScore: 0,
  awayScore: 0,
  homeFouls: 0,
  awayFouls: 0,
  homeTimeouts: 0,
  awayTimeouts: 0,
  period: 1,
  gameClockMs: 720000,
  shotClockMs: 24000,
  isGameClockRunning: false,
  isShotClockRunning: false,
  hasActiveMatch: false,
  homePlayers: [],
  awayPlayers: []
};

const contestState = {
  isActive: false,
  eventName: "3 Point Contest",
  playerName: "",
  teamName: "",
  teamColor: "#ea6324",
  score: 0,
  currentStation: 1,
  stationScores: [0, 0, 0, 0, 0],
  clockMs: 60000,
  status: "Ready"
};

let sponsorSlides = [];
let sponsorIndex = 0;
let sponsorTimer = null;
let displayMode = "scoreboard";
let playerListsSignature = "";
let threePointCleanupTimer = null;
let actionCleanupTimer = null;
let clockAnchorGameMs = state.gameClockMs;
let clockAnchorShotMs = state.shotClockMs;
let clockAnchorAt = performance.now();

function syncClockAnchor(sentAtUtc) {
  const sentAt = Number(sentAtUtc);
  const transitMs = Number.isFinite(sentAt) ? Math.max(0, Math.min(1000, Date.now() - sentAt)) : 0;
  clockAnchorGameMs = Math.max(0, Number(state.gameClockMs) - (state.isGameClockRunning ? transitMs : 0));
  clockAnchorShotMs = Math.max(0, Number(state.shotClockMs) - (state.isShotClockRunning ? transitMs : 0));
  clockAnchorAt = performance.now();
}

function currentGameClockMs() {
  const elapsed = state.isGameClockRunning ? performance.now() - clockAnchorAt : 0;
  return Math.max(0, clockAnchorGameMs - elapsed);
}

function currentShotClockMs() {
  const elapsed = state.isShotClockRunning ? performance.now() - clockAnchorAt : 0;
  return Math.max(0, clockAnchorShotMs - elapsed);
}

function setText(id, value) {
  document.getElementById(id).textContent = value;
}

function formatGameClock(ms) {
  const remainingMs = Math.max(0, Number(ms) || 0);
  if (remainingMs < 60000) {
    const totalTenths = Math.min(599, Math.ceil(remainingMs / 100));
    const seconds = Math.floor(totalTenths / 10).toString().padStart(2, "0");
    return seconds + "." + (totalTenths % 10);
  }

  const totalSeconds = Math.ceil(remainingMs / 1000);
  const minutes = Math.floor(totalSeconds / 60).toString().padStart(2, "0");
  const seconds = (totalSeconds % 60).toString().padStart(2, "0");
  return minutes + ":" + seconds;
}

function renderFoulPips(id, fouls) {
  const container = document.getElementById(id);
  container.replaceChildren();

  for (let index = 0; index < 5; index += 1) {
    const pip = document.createElement("span");
    pip.className = `pip${index < fouls ? " on" : ""}`;
    container.appendChild(pip);
  }
}

function renderPeriod() {
  const period = Math.max(1, Number(state.period) || 1);
  document.querySelectorAll("[data-period]").forEach(element => {
    const value = Number(element.dataset.period);
    element.classList.toggle("done", value < period);
    element.classList.toggle("now", value === period);
  });

  const overtime = document.getElementById("overtime");
  overtime.classList.toggle("visible", period >= 3);
  overtime.classList.toggle("now", period >= 3);
  const periodCodes = { 3: "OT", 4: "I1", 5: "I2", 6: "OTH" };
  overtime.textContent = periodCodes[period] || "OT";
}

function formatPeriod(period) {
  const value = Math.max(1, Number(period) || 1);
  if (value === 3) return "Overtime";
  if (value === 4) return "Intervallo 1";
  if (value === 5) return "Intervallo 2";
  if (value === 6) return "Others";
  return `${value}° Tempo`;
}

function renderSponsorMatchbar() {
  document.getElementById("sponsorHomeChip").style.background = state.homeColor;
  document.getElementById("sponsorAwayChip").style.background = state.awayColor;
  setText("sponsorHomeName", state.homeName);
  setText("sponsorAwayName", state.awayName);
  setText("sponsorHomeScore", state.homeScore);
  setText("sponsorAwayScore", state.awayScore);
  setText("sponsorGameClock", formatGameClock(currentGameClockMs()));
  setText("sponsorPeriod", formatPeriod(state.period));
  document.getElementById("sponsorMatchbar").classList.toggle("hidden", !state.hasActiveMatch);
}

function createPlayerRow(player) {
  const row = document.createElement("div");
  row.className = "stats-row stats-player";

  const number = document.createElement("span");
  number.className = "stats-number";
  number.textContent = player.jerseyNumber ?? "-";

  const name = document.createElement("span");
  name.className = "stats-player-name";
  name.textContent = player.name ?? "Giocatore";

  const points = document.createElement("span");
  points.className = "stats-value";
  points.textContent = player.points ?? 0;

  const fouls = document.createElement("span");
  fouls.className = "stats-value stats-fouls";
  fouls.textContent = player.fouls ?? 0;

  row.append(number, name, points, fouls);
  return row;
}

function renderPlayerList(id, players) {
  const list = document.getElementById(id);
  list.classList.remove("scrolling");
  list.replaceChildren(...players.map(createPlayerRow));

  requestAnimationFrame(() => {
    const viewport = list.parentElement;
    if (list.scrollHeight <= viewport.clientHeight + 4) {
      return;
    }

    const originalRows = [...list.children];
    originalRows.forEach(row => list.appendChild(row.cloneNode(true)));
    list.style.setProperty("--scroll-duration", `${Math.max(12, list.scrollHeight / 76)}s`);
    list.classList.add("scrolling");
  });
}

function renderPlayerStats() {
  document.getElementById("statsHomeTeam").style.setProperty("--team-color", state.homeColor);
  document.getElementById("statsAwayTeam").style.setProperty("--team-color", state.awayColor);
  document.getElementById("statsHomeChip").style.background = state.homeColor;
  document.getElementById("statsAwayChip").style.background = state.awayColor;

  setText("statsHomeName", state.homeName);
  setText("statsAwayName", state.awayName);
  setText("statsMatchHomeName", state.homeName);
  setText("statsMatchAwayName", state.awayName);
  setText("statsMatchHomeScore", state.homeScore);
  setText("statsMatchAwayScore", state.awayScore);
  setText("statsGameClock", formatGameClock(currentGameClockMs()));
  setText("statsPeriod", formatPeriod(state.period));

  const homePlayers = Array.isArray(state.homePlayers) ? state.homePlayers : [];
  const awayPlayers = Array.isArray(state.awayPlayers) ? state.awayPlayers : [];
  const signature = JSON.stringify([homePlayers, awayPlayers]);
  if (signature !== playerListsSignature) {
    playerListsSignature = signature;
    renderPlayerList("statsHomePlayers", homePlayers);
    renderPlayerList("statsAwayPlayers", awayPlayers);
  }
}

function render() {
  document.getElementById("homePanel").style.setProperty("--team-color", state.homeColor);
  document.getElementById("awayPanel").style.setProperty("--team-color", state.awayColor);

  setText("homeName", state.homeName);
  setText("awayName", state.awayName);
  setText("homeScore", state.homeScore);
  setText("awayScore", state.awayScore);
  renderFoulPips("homeFoulPips", Math.max(0, Number(state.homeFouls) || 0));
  renderFoulPips("awayFoulPips", Math.max(0, Number(state.awayFouls) || 0));
  renderPeriod();
  setText("gameClock", formatGameClock(currentGameClockMs()));
  setText("shotClock", Math.max(0, Math.ceil(currentShotClockMs() / 1000)));
  setText("status", state.isGameClockRunning ? "LIVE" : "PAUSA");

  document.getElementById("shotClock").classList.toggle("warning", currentShotClockMs() <= 5000 && state.isShotClockRunning);
  document.getElementById("status").classList.toggle("live", state.isGameClockRunning);
  renderSponsorMatchbar();
  renderPlayerStats();

  if (displayMode === "playerStats" && !state.hasActiveMatch) {
    showScoreboard();
  }
}

function showScoreboard() {
  displayMode = "scoreboard";
  document.getElementById("scoreboardView").classList.remove("hidden");
  document.getElementById("sponsorView").classList.add("hidden");
  document.getElementById("playerStatsView").classList.add("hidden");
  document.getElementById("contestView").classList.add("hidden");
  if (sponsorTimer) {
    clearInterval(sponsorTimer);
    sponsorTimer = null;
  }
}

function showSponsors(slides, intervalMs) {
  displayMode = "sponsors";
  sponsorSlides = Array.isArray(slides) ? slides : [];
  sponsorIndex = 0;
  const sponsorIntervalMs = Math.max(1000, Math.min(60000, Number(intervalMs) || 7000));
  document.getElementById("scoreboardView").classList.add("hidden");
  document.getElementById("sponsorView").classList.remove("hidden");
  document.getElementById("playerStatsView").classList.add("hidden");
  document.getElementById("contestView").classList.add("hidden");
  renderSponsorDots();
  renderSponsor();

  if (sponsorTimer) {
    clearInterval(sponsorTimer);
  }

  sponsorTimer = setInterval(() => {
    if (sponsorSlides.length === 0) {
      return;
    }

    sponsorIndex = (sponsorIndex + 1) % sponsorSlides.length;
    renderSponsor();
  }, sponsorIntervalMs);
}

function showPlayerStats() {
  if (!state.hasActiveMatch) {
    showScoreboard();
    return;
  }

  displayMode = "playerStats";
  document.getElementById("scoreboardView").classList.add("hidden");
  document.getElementById("sponsorView").classList.add("hidden");
  document.getElementById("playerStatsView").classList.remove("hidden");
  document.getElementById("contestView").classList.add("hidden");
  if (sponsorTimer) {
    clearInterval(sponsorTimer);
    sponsorTimer = null;
  }
  renderPlayerStats();
}

function renderContest() {
  document.getElementById("contestView").style.setProperty("--contest-color", contestState.teamColor || "#ea6324");
  setText("contestEventName", contestState.eventName || "Gara da 3 Punti");
  setText("contestTeamName", contestState.teamName || "Squadra");
  setText("contestPlayerName", contestState.playerName || "Giocatore");
  setText("contestScore", contestState.score ?? 0);
  setText("contestClock", Math.max(0, Math.ceil((contestState.clockMs ?? 0) / 1000)));
  const labels = { Ready: "Pronto", Live: "In corso", Paused: "In pausa", Finished: "Concluso" };
  setText("contestStatus", labels[contestState.status] || "Pronto");

  document.querySelectorAll(".contest-spot").forEach(spot => {
    spot.classList.toggle("active", Number(spot.dataset.station) === Number(contestState.currentStation));
  });

  const stations = document.getElementById("contestStations");
  stations.replaceChildren();
  (contestState.stationScores || [0, 0, 0, 0, 0]).forEach((score, index) => {
    const box = document.createElement("div");
    box.className = `contest-station-box${index + 1 === contestState.currentStation ? " current" : ""}`;
    const number = document.createElement("span");
    number.className = "contest-station-number";
    number.textContent = index + 1;
    const label = document.createElement("span");
    label.className = "contest-station-name";
    label.textContent = index + 1 === contestState.currentStation ? "Postazione attiva" : "Postazione";
    const rack = document.createElement("div");
    rack.className = "contest-ball-rack";
    for (let ballNumber = 1; ballNumber <= 5; ballNumber += 1) {
      const shot = (contestState.shots || []).find(item =>
        Number(item.stationNumber) === index + 1 && Number(item.ballNumber) === ballNumber);
      const result = (shot?.result || "Pending").toLowerCase();
      const ball = document.createElement("span");
      ball.className = "contest-ball " + result + (ballNumber === 5 ? " bonus" : "");
      ball.textContent = result === "missed" ? "X" : result === "made" ? "+" + (shot?.pointValue || (ballNumber === 5 ? 2 : 1)) : "";
      ball.setAttribute("aria-label", "Palla " + ballNumber + ": " + result);
      rack.appendChild(ball);
    }
    const value = document.createElement("strong");
    value.className = "contest-station-score";
    value.textContent = score;
    box.append(number, label, rack, value);
    stations.appendChild(box);
  });
}

function showContest() {
  displayMode = "contest";
  document.getElementById("scoreboardView").classList.add("hidden");
  document.getElementById("sponsorView").classList.add("hidden");
  document.getElementById("playerStatsView").classList.add("hidden");
  document.getElementById("contestView").classList.remove("hidden");
  renderContest();
}

function renderSponsorDots() {
  const dots = document.getElementById("sponsorDots");
  dots.replaceChildren();

  sponsorSlides.forEach((_, index) => {
    const dot = document.createElement("span");
    dot.className = `sponsor-dot${index === sponsorIndex ? " on" : ""}`;
    dots.appendChild(dot);
  });
}

function createThreePointParticles() {
  const particles = document.getElementById("threeParticles");
  particles.replaceChildren();
  const total = 26;

  for (let index = 0; index < total; index += 1) {
    const particle = document.createElement("span");
    const angle = (Math.PI * 2 * index) / total + Math.random() * .25;
    const distance = 180 + Math.random() * 360;
    particle.className = "three-particle";
    particle.style.setProperty("--particle-x", `${Math.cos(angle) * distance}px`);
    particle.style.setProperty("--particle-y", `${Math.sin(angle) * distance}px`);
    particle.style.setProperty("--particle-size", `${8 + Math.random() * 18}px`);
    particle.style.setProperty("--particle-rotation", `${300 + Math.random() * 720}deg`);
    particle.style.setProperty("--particle-delay", `${Math.random() * .12}s`);
    particles.appendChild(particle);
  }
}

function showThreePointCelebration(message) {
  stopActionCelebrations();
  const overlay = document.getElementById("threePointOverlay");
  const number = message.jerseyNumber == null ? "" : `#${message.jerseyNumber} · `;
  clearTimeout(threePointCleanupTimer);
  overlay.classList.remove("active");
  overlay.style.setProperty("--celebration-color", message.teamColor || "#ea6324");
  setText("threeTeamName", message.teamName || "Piazzetta Madness");
  setText("threePlayerName", `${number}${message.playerName || "Giocatore"}`);
  createThreePointParticles();

  void overlay.offsetWidth;
  overlay.classList.add("active");
  overlay.setAttribute("aria-hidden", "false");

  threePointCleanupTimer = setTimeout(() => {
    overlay.classList.remove("active");
    overlay.setAttribute("aria-hidden", "true");
  }, 1900);
}

function stopActionCelebrations() {
  clearTimeout(actionCleanupTimer);
  ["freeThrowOverlay", "dunkOverlay", "blockOverlay"].forEach(id => {
    const overlay = document.getElementById(id);
    overlay.classList.remove("active");
    overlay.setAttribute("aria-hidden", "true");
  });
}

function createActionParticles(id) {
  const container = document.getElementById(id);
  container.replaceChildren();

  for (let index = 0; index < 24; index += 1) {
    const particle = document.createElement("span");
    const angle = (Math.PI * 2 * index) / 24 + Math.random() * .24;
    const distance = 150 + Math.random() * 320;
    particle.className = "action-particle";
    particle.style.setProperty("--particle-x", `${Math.cos(angle) * distance}px`);
    particle.style.setProperty("--particle-y", `${Math.sin(angle) * distance}px`);
    particle.style.setProperty("--particle-size", `${8 + Math.random() * 16}px`);
    particle.style.setProperty("--particle-rotation", `${260 + Math.random() * 720}deg`);
    particle.style.setProperty("--particle-delay", `${Math.random() * .12}s`);
    container.appendChild(particle);
  }
}

function showPlayerCelebration(message) {
  const configurations = {
    freeThrow: { overlay: "freeThrowOverlay", team: "freeThrowTeam", player: "freeThrowPlayer", duration: 1800 },
    dunk: { overlay: "dunkOverlay", team: "dunkTeam", player: "dunkPlayer", particles: "dunkParticles", duration: 1900 },
    block: { overlay: "blockOverlay", team: "blockTeam", player: "blockPlayer", particles: "blockParticles", duration: 1900 }
  };
  const configuration = configurations[message.celebration];
  if (!configuration) {
    return;
  }

  clearTimeout(threePointCleanupTimer);
  document.getElementById("threePointOverlay").classList.remove("active");
  stopActionCelebrations();

  const overlay = document.getElementById(configuration.overlay);
  const number = message.jerseyNumber == null ? "" : `#${message.jerseyNumber} · `;
  overlay.style.setProperty("--action-color", message.teamColor || "#ea6324");
  setText(configuration.team, message.teamName || "Piazzetta Madness");
  setText(configuration.player, `${number}${message.playerName || "Giocatore"}`);
  if (configuration.particles) {
    createActionParticles(configuration.particles);
  }

  void overlay.offsetWidth;
  overlay.classList.add("active");
  overlay.setAttribute("aria-hidden", "false");
  actionCleanupTimer = setTimeout(stopActionCelebrations, configuration.duration);
}

function renderSponsor() {
  const sponsor = sponsorSlides[sponsorIndex];
  const image = document.getElementById("sponsorImage");
  const fallback = document.getElementById("sponsorFallback");

  if (!sponsor) {
    setText("sponsorName", "Piazzetta Madness");
    setText("sponsorDescription", "Sponsor in arrivo");
    setText("sponsorInitial", "P");
    image.classList.add("hidden");
    image.removeAttribute("src");
    fallback.classList.remove("hidden");
    renderSponsorDots();
    return;
  }

  setText("sponsorName", sponsor.name ?? "Sponsor");
  setText("sponsorDescription", sponsor.description ?? "Partner ufficiale");
  setText("sponsorInitial", (sponsor.name ?? "S").trim().charAt(0).toUpperCase() || "S");

  if (sponsor.imagePath) {
    image.src = `file:///${sponsor.imagePath.replaceAll("\\", "/")}`;
    image.classList.remove("hidden");
    fallback.classList.add("hidden");
  } else {
    image.classList.add("hidden");
    image.removeAttribute("src");
    fallback.classList.remove("hidden");
  }

  renderSponsorDots();
}

function renderClockFrame() {
  const gameClockMs = currentGameClockMs();
  const shotClockMs = currentShotClockMs();
  const formattedGameClock = formatGameClock(gameClockMs);

  setText("gameClock", formattedGameClock);
  setText("sponsorGameClock", formattedGameClock);
  setText("statsGameClock", formattedGameClock);
  setText("shotClock", Math.max(0, Math.ceil(shotClockMs / 1000)));
  document.getElementById("shotClock").classList.toggle("warning", shotClockMs <= 5000 && state.isShotClockRunning);

  requestAnimationFrame(renderClockFrame);
}

window.chrome?.webview?.addEventListener("message", event => {
  const message = event.data;
  if (message?.type === "threePointCelebration") {
    showThreePointCelebration(message);
    return;
  }

  if (message?.type === "playerCelebration") {
    showPlayerCelebration(message);
    return;
  }

  if (message?.type === "contestState") {
    Object.assign(contestState, message.state);
    renderContest();
    return;
  }

  if (message?.type === "displayMode") {
    if (message.mode === "sponsors") {
      showSponsors(message.sponsors, message.sponsorIntervalMs);
    } else if (message.mode === "playerStats") {
      showPlayerStats();
    } else if (message.mode === "contest") {
      showContest();
    } else {
      showScoreboard();
    }

    return;
  }

  if (message?.type === "scoreboardState") {
    Object.assign(state, message.state);
    syncClockAnchor(message.sentAtUtc);
    render();
  }
});

syncClockAnchor(Date.now());
render();
requestAnimationFrame(renderClockFrame);
