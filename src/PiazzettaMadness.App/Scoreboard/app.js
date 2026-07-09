const state = {
  homeName: "A2",
  awayName: "A4",
  homeShortName: "A2",
  awayShortName: "A4",
  homeColor: "#f77f00",
  awayColor: "#457b9d",
  homeSecondaryColor: "#fffefd",
  awaySecondaryColor: "#fffefd",
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
  clockMs: 90000,
  status: "Ready"
};

let sponsorSlides = [];
let sponsorIndex = 0;
let sponsorTimer = null;
let merchandiseSlides = [];
let merchandiseIndex = 0;
let merchandiseTimer = null;
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

function formatContestClock(ms) {
  const totalTenths = Math.min(900, Math.ceil(Math.max(0, Number(ms) || 0) / 100));
  const seconds = Math.floor(totalTenths / 10).toString().padStart(2, "0");
  return seconds + "." + (totalTenths % 10);
}

function renderPips(id, count, total, slots = total) {
  const container = document.getElementById(id);
  container.replaceChildren();

  for (let index = 0; index < slots; index += 1) {
    const pip = document.createElement("span");
    pip.className = `pip${index < total ? "" : " spacer"}${index < count ? " on" : ""}`;
    container.appendChild(pip);
  }
}

function renderFoulPips(id, fouls) {
  renderPips(id, fouls, 5);
}

function renderTimeoutPips(id, timeouts) {
  renderPips(id, timeouts, 2);
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
  const periodCodes = { 3: "OT", 4: "INT", 5: "WARM" };
  overtime.textContent = periodCodes[period] || "OT";
}

function formatPeriod(period) {
  const value = Math.max(1, Number(period) || 1);
  if (value === 3) return "Overtime";
  if (value === 4) return "Intervallo";
  if (value === 5) return "Riscaldamento";
  return `${value}${String.fromCharCode(176)} Tempo`;
}

function renderSponsorMatchbar() {
  document.getElementById("sponsorHomeChip").style.background = state.homeColor;
  document.getElementById("sponsorAwayChip").style.background = state.awayColor;
  setText("sponsorHomeName", state.homeShortName || state.homeName);
  setText("sponsorAwayName", state.awayShortName || state.awayName);
  setText("sponsorHomeScore", state.homeScore);
  setText("sponsorAwayScore", state.awayScore);
  setText("sponsorGameClock", formatGameClock(currentGameClockMs()));
  setText("sponsorPeriod", formatPeriod(state.period));
  document.getElementById("sponsorMatchbar").classList.toggle("hidden", !state.hasActiveMatch);
}
function renderQrMatchbar() {
  document.getElementById("qrHomeChip").style.background = state.homeColor;
  document.getElementById("qrAwayChip").style.background = state.awayColor;
  setText("qrHomeName", state.homeShortName || state.homeName);
  setText("qrAwayName", state.awayShortName || state.awayName);
  setText("qrHomeScore", state.homeScore);
  setText("qrAwayScore", state.awayScore);
  setText("qrGameClock", formatGameClock(currentGameClockMs()));
  setText("qrPeriod", formatPeriod(state.period));
  document.getElementById("qrMatchbar").classList.toggle("hidden", !state.hasActiveMatch);
}
function renderMerchandiseMatchbar() {
  document.getElementById("merchandiseHomeChip").style.background = state.homeColor;
  document.getElementById("merchandiseAwayChip").style.background = state.awayColor;
  setText("merchandiseHomeName", state.homeShortName || state.homeName);
  setText("merchandiseAwayName", state.awayShortName || state.awayName);
  setText("merchandiseHomeScore", state.homeScore);
  setText("merchandiseAwayScore", state.awayScore);
  setText("merchandiseGameClock", formatGameClock(currentGameClockMs()));
  setText("merchandisePeriod", formatPeriod(state.period));
  document.getElementById("merchandiseMatchbar").classList.toggle("hidden", !state.hasActiveMatch);
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
  list.style.removeProperty("--scroll-duration");
  list.replaceChildren(...players.map(createPlayerRow));

  requestAnimationFrame(() => requestAnimationFrame(() => {
    const viewport = list.parentElement;
    if (!viewport || list.scrollHeight <= viewport.clientHeight + 4) {
      return;
    }

    const originalRows = [...list.children];
    originalRows.forEach(row => list.appendChild(row.cloneNode(true)));
    list.style.setProperty("--scroll-duration", `${Math.max(14, list.scrollHeight / 70)}s`);
    list.classList.add("scrolling");
  }));
}

function renderPlayerStats(force = false) {
  document.getElementById("statsHomeTeam").style.setProperty("--team-color", state.homeColor);
  document.getElementById("statsAwayTeam").style.setProperty("--team-color", state.awayColor);
  document.getElementById("statsHomeChip").style.background = state.homeColor;
  document.getElementById("statsAwayChip").style.background = state.awayColor;

  setText("statsHomeName", state.homeName);
  setText("statsAwayName", state.awayName);
  setText("statsMatchHomeName", state.homeShortName || state.homeName);
  setText("statsMatchAwayName", state.awayShortName || state.awayName);
  setText("statsMatchHomeScore", state.homeScore);
  setText("statsMatchAwayScore", state.awayScore);
  setText("statsGameClock", formatGameClock(currentGameClockMs()));
  setText("statsPeriod", formatPeriod(state.period));

  const homePlayers = Array.isArray(state.homePlayers) ? state.homePlayers : [];
  const awayPlayers = Array.isArray(state.awayPlayers) ? state.awayPlayers : [];
  const signature = JSON.stringify([homePlayers, awayPlayers]);
  if (force || signature !== playerListsSignature) {
    playerListsSignature = signature;
    renderPlayerList("statsHomePlayers", homePlayers);
    renderPlayerList("statsAwayPlayers", awayPlayers);
  }
}

function render() {
  document.getElementById("homePanel").style.setProperty("--team-color", state.homeColor);
  document.getElementById("awayPanel").style.setProperty("--team-color", state.awayColor);
  document.getElementById("homePanel").style.setProperty("--team-text-color", state.homeSecondaryColor || "#fffefd");
  document.getElementById("awayPanel").style.setProperty("--team-text-color", state.awaySecondaryColor || "#fffefd");

  setText("homeName", state.homeName);
  setText("awayName", state.awayName);
  document.getElementById("homeName").setAttribute("data-team-name", state.homeName || "");
  document.getElementById("awayName").setAttribute("data-team-name", state.awayName || "");
  setText("homeScore", state.homeScore);
  setText("awayScore", state.awayScore);
  renderFoulPips("homeFoulPips", Math.max(0, Number(state.homeFouls) || 0));
  renderFoulPips("awayFoulPips", Math.max(0, Number(state.awayFouls) || 0));
  renderTimeoutPips("homeTimeoutPips", Math.max(0, Number(state.homeTimeouts) || 0));
  renderTimeoutPips("awayTimeoutPips", Math.max(0, Number(state.awayTimeouts) || 0));
  renderPeriod();
  setText("gameClock", formatGameClock(currentGameClockMs()));
  setText("shotClock", Math.max(0, Math.ceil(currentShotClockMs() / 1000)));
  setText("status", state.isGameClockRunning ? "LIVE" : "PAUSA");

  document.getElementById("shotClock").classList.toggle("warning", currentShotClockMs() <= 5000 && state.isShotClockRunning);
  document.getElementById("status").classList.toggle("live", state.isGameClockRunning);
  renderSponsorMatchbar();
  renderQrMatchbar();
  renderMerchandiseMatchbar();
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
  document.getElementById("qrView").classList.add("hidden");
  document.getElementById("merchandiseView").classList.add("hidden");
  if (sponsorTimer) {
    clearInterval(sponsorTimer);
    sponsorTimer = null;
  }
  if (merchandiseTimer) {
    clearInterval(merchandiseTimer);
    merchandiseTimer = null;
  }
}

function showSponsors(slides, intervalMs) {
  displayMode = "sponsors";
  sponsorSlides = Array.isArray(slides) ? slides : [];
  sponsorIndex = 0;
  const sponsorIntervalMs = Math.max(1000, Math.min(60000, Number(intervalMs) || 2000));
  document.getElementById("scoreboardView").classList.add("hidden");
  document.getElementById("sponsorView").classList.remove("hidden");
  document.getElementById("playerStatsView").classList.add("hidden");
  document.getElementById("contestView").classList.add("hidden");
  document.getElementById("qrView").classList.add("hidden");
  document.getElementById("merchandiseView").classList.add("hidden");
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


function showMerchandise(items, intervalMs) {
  displayMode = "merchandise";
  merchandiseSlides = Array.isArray(items) ? items : [];
  merchandiseIndex = 0;
  const merchandiseIntervalMs = Math.max(1000, Math.min(60000, Number(intervalMs) || 8000));
  document.getElementById("scoreboardView").classList.add("hidden");
  document.getElementById("sponsorView").classList.add("hidden");
  document.getElementById("merchandiseView").classList.remove("hidden");
  document.getElementById("playerStatsView").classList.add("hidden");
  document.getElementById("contestView").classList.add("hidden");
  document.getElementById("qrView").classList.add("hidden");
  renderMerchandiseDots();
  renderMerchandise();

  if (sponsorTimer) {
    clearInterval(sponsorTimer);
    sponsorTimer = null;
  }
  if (merchandiseTimer) {
    clearInterval(merchandiseTimer);
  }

  merchandiseTimer = setInterval(() => {
    if (merchandiseSlides.length === 0) {
      return;
    }

    merchandiseIndex = (merchandiseIndex + 1) % merchandiseSlides.length;
    renderMerchandise();
  }, merchandiseIntervalMs);
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
  document.getElementById("qrView").classList.add("hidden");
  document.getElementById("merchandiseView").classList.add("hidden");
  if (sponsorTimer) {
    clearInterval(sponsorTimer);
    sponsorTimer = null;
  }
  if (merchandiseTimer) {
    clearInterval(merchandiseTimer);
    merchandiseTimer = null;
  }
  renderPlayerStats(true);
}

function renderContest() {
  document.getElementById("contestView").style.setProperty("--contest-color", contestState.teamColor || "#ea6324");
  setText("contestEventName", contestState.eventName || "Gara da 3 Punti");
  setText("contestTeamName", contestState.teamName || "Squadra");
  setText("contestPlayerName", contestState.playerName || "Giocatore");
  setText("contestScore", contestState.score ?? 0);
  setText("contestClock", formatContestClock(contestState.clockMs ?? 0));
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
    const rack = document.createElement("div");
    rack.className = "contest-ball-rack";
    for (let ballNumber = 1; ballNumber <= 5; ballNumber += 1) {
      const shot = (contestState.shots || []).find(item =>
        Number(item.stationNumber) === index + 1 && Number(item.ballNumber) === ballNumber);
      const result = (shot?.result || "Pending").toLowerCase();
      const ball = document.createElement("span");
      const isBonus = ballNumber === 5;
      const isMadeBonus = isBonus && result === "made";
      ball.className = "contest-ball " + result + (isBonus ? " bonus" : "") + (isMadeBonus ? " logo" : "");
      ball.textContent = result === "missed" ? "X" : isMadeBonus ? "" : result === "made" ? "+" + (shot?.pointValue || (isBonus ? 2 : 1)) : "";
      ball.setAttribute("aria-label", "Palla " + ballNumber + ": " + result);
      rack.appendChild(ball);
    }
    const value = document.createElement("strong");
    value.className = "contest-station-score";
    value.textContent = score;
    box.append(number, rack, value);
    stations.appendChild(box);
  });
}

function showQrCode() {
  displayMode = "qrCode";
  document.getElementById("scoreboardView").classList.add("hidden");
  document.getElementById("sponsorView").classList.add("hidden");
  document.getElementById("playerStatsView").classList.add("hidden");
  document.getElementById("contestView").classList.add("hidden");
  document.getElementById("qrView").classList.remove("hidden");
  if (sponsorTimer) {
    clearInterval(sponsorTimer);
    sponsorTimer = null;
  }
  if (merchandiseTimer) {
    clearInterval(merchandiseTimer);
    merchandiseTimer = null;
  }
  renderQrMatchbar();
}

function showContest() {
  displayMode = "contest";
  document.getElementById("scoreboardView").classList.add("hidden");
  document.getElementById("sponsorView").classList.add("hidden");
  document.getElementById("playerStatsView").classList.add("hidden");
  document.getElementById("contestView").classList.remove("hidden");
  document.getElementById("qrView").classList.add("hidden");
  document.getElementById("merchandiseView").classList.add("hidden");
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
  const number = message.jerseyNumber == null ? "" : `#${message.jerseyNumber} - `;
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
  const number = message.jerseyNumber == null ? "" : `#${message.jerseyNumber} - `;
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


function renderMerchandiseDots() {
  const dots = document.getElementById("merchandiseDots");
  dots.replaceChildren();

  merchandiseSlides.forEach((_, index) => {
    const dot = document.createElement("span");
    dot.className = `sponsor-dot${index === merchandiseIndex ? " on" : ""}`;
    dots.appendChild(dot);
  });
}

function formatMerchandisePrice(value) {
  const amount = Number(value);
  if (!Number.isFinite(amount) || amount <= 0) {
    return "";
  }

  return new Intl.NumberFormat("it-IT", { style: "currency", currency: "EUR" }).format(amount);
}

function renderMerchandise() {
  const item = merchandiseSlides[merchandiseIndex];
  const image = document.getElementById("merchandiseImage");
  const fallback = document.getElementById("merchandiseFallback");
  const price = document.getElementById("merchandisePrice");

  if (!item) {
    setText("merchandiseName", "Piazzetta Madness");
    setText("merchandiseDescription", "Merchandising ufficiale in arrivo");
    setText("merchandiseInitial", "M");
    price.classList.add("hidden");
    image.classList.add("hidden");
    image.removeAttribute("src");
    fallback.classList.remove("hidden");
    renderMerchandiseDots();
    return;
  }

  setText("merchandiseName", item.name ?? "Merchandising");
  setText("merchandiseDescription", item.description ?? "Merchandising ufficiale");
  setText("merchandiseInitial", (item.name ?? "M").trim().charAt(0).toUpperCase() || "M");
  const formattedPrice = formatMerchandisePrice(item.price);
  if (formattedPrice) {
    setText("merchandisePrice", formattedPrice);
    price.classList.remove("hidden");
  } else {
    price.classList.add("hidden");
  }

  if (item.imagePath) {
    image.src = `file:///${item.imagePath.replaceAll("\\\\", "/")}`;
    image.classList.remove("hidden");
    fallback.classList.add("hidden");
  } else {
    image.classList.add("hidden");
    image.removeAttribute("src");
    fallback.classList.remove("hidden");
  }

  renderMerchandiseDots();
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
  setText("qrGameClock", formattedGameClock);
  setText("merchandiseGameClock", formattedGameClock);
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
    } else if (message.mode === "merchandise") {
      showMerchandise(message.merchandise, message.merchandiseIntervalMs);
    } else if (message.mode === "playerStats") {
      showPlayerStats();
    } else if (message.mode === "contest") {
      showContest();
    } else if (message.mode === "qrCode") {
      showQrCode();
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
