const canvas = document.getElementById('game-canvas');
const ctx = canvas.getContext('2d');
const hordeKillsElement = document.getElementById('horde-kills');
const bossKillsElement = document.getElementById('boss-kills');
const gameTimeElement = document.getElementById('game-time');
const startGameButton = document.getElementById('start-game-button');
const messageElement = document.getElementById('game-message');

const apiurl = 'https://hordeapi-csexhfc9ekdda2ej.swedencentral-01.azurewebsites.net';

let gameState = null;
let gameLoopRunning = false;
let moveInterval; // Variable to store the movement interval timer
let currentDirection = null; // Track current movement direction

// Get reference to the movement control area
const movementControl = document.getElementById('movement-control');

// --- Touch and Mouse event listeners for movement control ---
movementControl.addEventListener('touchstart', startMove);
movementControl.addEventListener('mousedown', startMove);
movementControl.addEventListener('touchend', stopMove);
movementControl.addEventListener('mouseup', stopMove);
movementControl.addEventListener('touchcancel', stopMove);
movementControl.addEventListener('mouseleave', stopMove); // Stop if mouse leaves control area

startGameButton.addEventListener('click', startGame);

async function startGame() {
    if (gameLoopRunning) { // Game is running, so stop it
        stopGame(); // Call stopGame function
        startGameButton.textContent = "Start Game"; // Change button text to "Start Game"
    } else { // Game is not running, so start it
        console.log("Starting new game");
        const response = await fetch(`${apiurl}/Game/start`, { method: 'POST' });
        gameState = await response.json();
        console.log("Game state after starting:", gameState);
        setCanvasSize(gameState.screenWidth, gameState.screenHeight);
        gameLoopRunning = true;
        gameLoop();
        startGameButton.textContent = "Stop Game"; // Change button text to "Stop Game"
    }
}

function stopGame() {
    console.log("Stopping game");
    gameLoopRunning = false; // Stop the animation loop
    stopMove(); // Ensure movement interval is cleared when game stops
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

    // Only move if direction is different from current direction to prevent redundant calls
    if (direction !== currentDirection) {
        currentDirection = direction; // Update current direction

        console.log(`Moving soldier ${direction}`);
        const response = await fetch(`${apiurl}/Game/soldier/move`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ direction: direction })
        });

        if (response.ok) {
            gameState = await response.json();
            console.log("Game state after moving soldier:", gameState);
        } else {
            console.error("Failed to move soldier:", response.statusText);
        }
    }
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

    // Draw bosses
    if (gameState.bosses && gameState.bosses.length > 0) {
        gameState.bosses.forEach(boss => {
            const bossImg = new Image();
            bossImg.src = 'boss.png';
            bossImg.onload = () => {
                ctx.drawImage(bossImg, boss.x, boss.y, 50, 50);
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
            shotImg.src = 'shot.png';
            shotImg.onload = () => {
                ctx.drawImage(shotImg, shot.x, shot.y, 20, 20);
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

async function gameLoop() {
    if (!gameLoopRunning) return; // Stop the loop if gameLoopRunning is false

    await getGameState();
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

function startMove(event) {
    event.preventDefault(); // Prevent default touch/mouse behavior
    let touchX;

    if (event.type === 'touchstart') {
        touchX = event.touches[0].clientX; // Get touch x coordinate
    } else { // mousedown
        touchX = event.clientX;           // Get mouse x coordinate
    }

    const controlRect = movementControl.getBoundingClientRect();
    const controlCenterX = controlRect.left + controlRect.width / 2;

    let direction;
    if (touchX < controlCenterX) {
        direction = 'left';
    } else {
        direction = 'right';
    }
    if (direction !== currentDirection) {
        stopMove(); // Stop any existing interval before starting a new one
        currentDirection = direction;
        // Start continuous movement using setInterval
        moveInterval = setInterval(() => {
            moveSoldier(direction); // Call your existing moveSoldier function
        }, 100); // Adjust interval (milliseconds) for movement speed
    }
}


function stopMove() {
    clearInterval(moveInterval); // Stop the continuous movement interval
    currentDirection = null; // Reset current direction
}


// Initial canvas size setup (optional - can be set in HTML)
setCanvasSize(300, 600);