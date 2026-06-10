import pygame
import os
import math

# Initialize Pygame
pygame.init()

# Create assets directories if they don't exist
os.makedirs("assets/characters", exist_ok=True)
os.makedirs("assets/abilities", exist_ok=True)

# Character size
CHARACTER_SIZE = (120, 120)


def create_character_image(name, color):
    # Create a surface for the character
    surface = pygame.Surface(CHARACTER_SIZE, pygame.SRCALPHA)

    # Draw a simple character shape
    center = (CHARACTER_SIZE[0] // 2, CHARACTER_SIZE[1] // 2)
    radius = 40

    # Body
    pygame.draw.circle(surface, color, center, radius)

    # Eyes
    eye_color = (255, 255, 255)
    eye_radius = 8
    left_eye = (center[0] - 15, center[1] - 10)
    right_eye = (center[0] + 15, center[1] - 10)
    pygame.draw.circle(surface, eye_color, left_eye, eye_radius)
    pygame.draw.circle(surface, eye_color, right_eye, eye_radius)

    # Pupils
    pupil_color = (0, 0, 0)
    pupil_radius = 4
    pygame.draw.circle(surface, pupil_color, left_eye, pupil_radius)
    pygame.draw.circle(surface, pupil_color, right_eye, pupil_radius)

    # Save the image
    pygame.image.save(surface, f"assets/characters/{name}.png")


def create_ability_animation(ability_name, color, num_frames=8):
    # Create directory for the ability
    ability_dir = f"assets/abilities/{ability_name}"
    os.makedirs(ability_dir, exist_ok=True)

    for frame in range(num_frames):
        surface = pygame.Surface(CHARACTER_SIZE, pygame.SRCALPHA)
        center = (CHARACTER_SIZE[0] // 2, CHARACTER_SIZE[1] // 2)

        # Different effects for different abilities
        if ability_name == "slash":
            # Create a slash effect
            angle = (frame / num_frames) * math.pi
            start_pos = (center[0] - 30, center[1])
            end_pos = (
                center[0] + 30 * math.cos(angle),
                center[1] + 30 * math.sin(angle),
            )
            pygame.draw.line(surface, color, start_pos, end_pos, 5)

        elif ability_name == "fireball":
            # Create a fireball effect
            radius = 20 + frame * 2
            alpha = 255 - (frame * 255 // num_frames)
            color_with_alpha = (*color, alpha)
            pygame.draw.circle(surface, color_with_alpha, center, radius)

        elif ability_name == "bash":
            # Create a bash effect
            radius = 40 + frame * 3
            alpha = 255 - (frame * 255 // num_frames)
            color_with_alpha = (*color, alpha)
            pygame.draw.circle(surface, color_with_alpha, center, radius, 5)

        else:
            # Generic ability effect
            radius = 30 + frame * 2
            alpha = 255 - (frame * 255 // num_frames)
            color_with_alpha = (*color, alpha)
            pygame.draw.circle(surface, color_with_alpha, center, radius)

        # Save the frame
        pygame.image.save(surface, f"{ability_dir}/frame{frame + 1}.png")


# Generate character images
create_character_image("player", (80, 80, 200))  # Blue player
create_character_image("opponent", (200, 80, 80))  # Red opponent

# Generate ability animations
abilities = {
    "slash": (255, 255, 255),  # White slash
    "fireball": (255, 100, 0),  # Orange fireball
    "bash": (150, 75, 0),  # Brown bash
    "vampiro": (200, 0, 0),  # Red vampiro
    "parry": (200, 200, 0),  # Yellow parry
    "riposte": (255, 100, 100),  # Light red riposte
    "kick": (100, 100, 100),  # Gray kick
    "interrupt_cast": (150, 150, 150),  # Light gray interrupt
    "autoattack": (200, 200, 200),  # Light gray autoattack
}

for ability_name, color in abilities.items():
    create_ability_animation(ability_name, color)

print("Assets generated successfully!")
