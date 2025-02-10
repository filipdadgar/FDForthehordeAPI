const canvas = document.getElementById('game-canvas');
const ctx = canvas.getContext('2d');
const hordeKillsElement = document.getElementById('horde-kills');
const bossKillsElement = document.getElementById('boss-kills');
const gameTimeElement = document.getElementById('game-time');
const startGameButton = document.getElementById('start-game-button');
const messageElement = document.getElementById('game-message');

const apiurl = 'https://hordeapi-csexhfc9ekdda2ej.swedencentral-01.azurewebsites.net';
//const apiurl = 'http://localhost:5105';


let gameState = null;
let gameLoopRunning = false;
let moveInterval = null;

// Get references to touch buttons
const leftButton = document.getElementById('left-button');
const rightButton = document.getElementById('right-button');

// --- Touch and Mouse event listeners for left button ---
leftButton.addEventListener('touchstart', (event) => {
    event.preventDefault(); // Prevent default touch behavior
    startMoving('left');
});
leftButton.addEventListener('touchend', (event) => {
    event.preventDefault();
    stopMoving();
});
leftButton.addEventListener('mousedown', (event) => { // For desktop mouse clicks
    event.preventDefault();
    startMoving('left');
});
leftButton.addEventListener('mouseup', (event) => {
    event.preventDefault();
    stopMoving();
});
leftButton.addEventListener('mouseleave', (event) => {
    event.preventDefault();
    stopMoving();
});

// --- Touch and Mouse event listeners for right button ---
rightButton.addEventListener('touchstart', (event) => {
    event.preventDefault(); // Prevent default touch behavior
    startMoving('right');
});
rightButton.addEventListener('touchend', (event) => {
    event.preventDefault();
    stopMoving();
});
rightButton.addEventListener('mousedown', (event) => { // For desktop mouse clicks
    event.preventDefault();
    startMoving('right');
});
rightButton.addEventListener('mouseup', (event) => {
    event.preventDefault();
    stopMoving();
});
rightButton.addEventListener('mouseleave', (event) => {
    event.preventDefault();
    stopMoving();
});

startGameButton.addEventListener('click', startGame);

async function startGame() {
    if (gameLoopRunning) { // Game is running, so stop it
        stopGame(); // Call stopGame function
        startGameButton.textContent = "Start Game"; // Change button text to "Start Game"
    } else { // Game is not running, so start it
        //console.log("Starting new game");
        const response = await fetch(`${apiurl}/Game/start`, { method: 'POST' });
        gameState = await response.json();
        //console.log("Game state after starting:", gameState);
        setCanvasSize(gameState.screenWidth, gameState.screenHeight);
        gameLoopRunning = true;
        gameLoop();
        startGameButton.textContent = "Stop Game"; // Change button text to "Stop Game"
    }
}

function stopGame() {
    // console.log("Stopping game");
    gameLoopRunning = false; // Stop the animation loop
    // Optional: Send a request to backend to explicitly stop backend loop if needed
    fetch(`${apiurl}/Game/stop`, { method: 'POST' });
}

async function getGameState() {
    if (!gameLoopRunning) return; // Stop fetching state if game loop is not running
    const response = await fetch(`${apiurl}/Game/state`);
    gameState = await response.json();
    if (gameState && gameState.isGameOver) {
        stopGame(); // Stop the frontend loop if game over is detected in backend state
        alert(gameState.message);
    }
}

async function moveSoldier(direction) {
    if (gameState && gameState.isGameOver) {
        alert("Game Over! Cannot move the soldier.");
        return;
    }

    // console.log(`Moving soldier ${direction}`);
    const response = await fetch(`${apiurl}/Game/soldier/move`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ direction: direction })
    });

    if (response.ok) {
        gameState = await response.json();
       // console.log("Game state after moving soldier:", gameState);
    } else {
       // console.error("Failed to move soldier:", response.statusText);
    }
}

function startMoving(direction) {
    if (moveInterval) return; // Prevent multiple intervals
    moveInterval = setInterval(() => moveSoldier(direction), 100); // Move every 100ms
}

function stopMoving() {
    clearInterval(moveInterval);
    moveInterval = null;
}

function drawGame() {
    if (!gameState) return;

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Draw soldier
    if (gameState.soldier) {
        const soldierImg = new Image();
        soldierImg.src = 'soldier.png';
        soldierImg.onload = () => {
            ctx.drawImage(soldierImg, gameState.soldier.x, gameState.soldier.y, 50, 50);
        };

    }

    if (gameState.bonusSoldiers && gameState.bonusSoldiers.length > 0) { // Draw bonus soldiers
        gameState.bonusSoldiers.forEach(bonusSoldier => {
            const soldierImg = new Image(); // Can reuse soldier image
            soldierImg.src = 'soldier.png';
            soldierImg.onload = () => {
                ctx.drawImage(soldierImg, bonusSoldier.x, bonusSoldier.y, 50, 50);
            };
        });
    }

    // Draw hordes
    if (gameState.hordes && gameState.hordes.length > 0) {
        gameState.hordes.forEach(horde => {
            const hordeImg = new Image();
            hordeImg.src = 'horde.png';
            hordeImg.onload = () => {
                ctx.drawImage(hordeImg, horde.x, horde.y, 30, 30);
            };
        });
    }

    // Draw bosses with hitpoints
    if (gameState.bosses && gameState.bosses.length > 0) {
        gameState.bosses.forEach(boss => {
            const bossImg = new Image();
            bossImg.src = 'boss.png';
            bossImg.onload = () => {
                ctx.drawImage(bossImg, boss.x, boss.y, 50, 50);
                ctx.fillStyle = 'red';
                ctx.font = '16px Arial';
                ctx.fillText(boss.hitPoints, boss.x + 15, boss.y - 10); // Adjust position as needed
            };
        });
    }

    // Draw chest
    if (gameState.chest && !gameState.chest.isDestroyed) {
        const chestImg = new Image();
        chestImg.src = 'chest.png';
        chestImg.onload = () => {
            ctx.drawImage(chestImg, gameState.chest.x, gameState.chest.y, 40, 40);
        };
    }

    // --- Draw Shots ---
    if (gameState.shots && gameState.shots.length > 0) {
        gameState.shots.forEach(shot => {
            const shotImg = new Image();
            shotImg.src = 'big.png';
            shotImg.onload = () => {
                ctx.drawImage(shotImg, shot.x, shot.y, 50, 50);
            };
        });
    }

    // Update game info
    hordeKillsElement.textContent = gameState.hordeKills;
    bossKillsElement.textContent = gameState.bossKills;
    gameTimeElement.textContent = gameState.gameTime ? Math.floor(parseFloat(gameState.gameTime.split(':')[2])) : 0;
    if (gameState.message) {
        messageElement.textContent = gameState.message; // Display message if available
    } else {
        messageElement.textContent = ''; // Clear message if no message from backend
    }
}

function moveShotsUpward() {
    if (!gameState) return;

    const nextShots = [];
    gameState.shots.forEach(shot => {
        shot.y += shot.speedY; // Move shot up

        // Check for collision with hordes
        gameState.hordes.forEach(horde => {
            if (shot.x >= horde.x && shot.x <= horde.x + 30 && shot.y >= horde.y && shot.y <= horde.y + 30) {
                horde.hitPoints--;
                if (horde.hitPoints <= 0) {
                    gameState.hordeKills++;
                    gameState.hordes = gameState.hordes.filter(h => h !== horde);
                }
            }
        });

        // Check for collision with bosses
        gameState.bosses.forEach(boss => {
            if (shot.x >= boss.x && shot.x <= boss.x + 50 && shot.y >= boss.y && shot.y <= boss.y + 50) {
                boss.hitPoints--;
                if (boss.hitPoints <= 0) {
                    gameState.bossKills++;
                    gameState.bosses = gameState.bosses.filter(b => b !== boss);
                }
            }
        });

        // Keep shot if it is still on screen and did not hit any target
        if (shot.y > 0) {
            nextShots.push(shot);
        }
    });

    gameState.shots = nextShots;
}

async function gameLoop() {
    if (!gameLoopRunning) return; // Stop the loop if gameLoopRunning is false

    await getGameState();
    moveShotsUpward(); // Move shots upward in each frame
    drawGame();
    requestAnimationFrame(gameLoop);
}

document.addEventListener('keydown', (event) => {
    if (event.key === 'ArrowLeft') {
        moveSoldier('left');
    } else if (event.key === 'ArrowRight') {
        moveSoldier('right');
    }
});

function setCanvasSize(screenWidth, screenHeight) {
    canvas.width = screenWidth;
    canvas.height = screenHeight;
}

// Initial canvas size setup (optional - can be set in HTML)
setCanvasSize(300, 600);