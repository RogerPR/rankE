import pygame
from constants import MAX_HP, MAX_HP_OPPONENT, GLOBAL_COOLDOWN_TICKS, STANCE_ACTION
import os
import math
import time

FONT_SIZE = 24
SMALL_FONT_SIZE = 16
ABILITY_BOX_WIDTH = 90
ABILITY_BOX_HEIGHT = 60
HP_BAR_WIDTH = 200
HP_BAR_HEIGHT = 20
CHARACTER_SIZE = (360, 360)  # Size for character images
TOP_PADDING = 40  # Added top padding constant

pygame.init()
font = pygame.font.SysFont("Arial", FONT_SIZE, bold=True)
small_font = pygame.font.SysFont("Arial", SMALL_FONT_SIZE)


# Load character images
def load_character_images():
    images = {}
    character_dir = "assets/characters"
    if os.path.exists(character_dir):
        for filename in os.listdir(character_dir):
            if filename.endswith((".png", ".jpg")):
                name = os.path.splitext(filename)[0]
                path = os.path.join(character_dir, filename)
                try:
                    # Load and resize the image to the correct dimensions
                    original_image = pygame.image.load(path)
                    images[name] = pygame.transform.scale(
                        original_image, CHARACTER_SIZE
                    )
                except Exception as e:
                    pass
    return images


# Load ability animations
def load_ability_animations():
    animations = {}
    ability_dir = "assets/abilities"
    if os.path.exists(ability_dir):
        for ability_name in os.listdir(ability_dir):
            ability_path = os.path.join(ability_dir, ability_name)
            if os.path.isdir(ability_path):
                frames = []
                for frame_file in sorted(os.listdir(ability_path)):
                    if frame_file.endswith((".png", ".jpg")):
                        frame_path = os.path.join(ability_path, frame_file)
                        frame = pygame.image.load(frame_path)
                        # Scale ability animations to half the character size
                        frame = pygame.transform.scale(
                            frame, (CHARACTER_SIZE[0] // 2, CHARACTER_SIZE[1] // 2)
                        )
                        frames.append(frame)
                if frames:
                    animations[ability_name] = frames
    return animations


# Load background image
def load_background():
    background_path = "assets/background.png"
    if os.path.exists(background_path):
        try:
            background = pygame.image.load(background_path)
            return background
        except Exception as e:
            pass
    return None


# Initialize images and animations
character_images = load_character_images()
ability_animations = load_ability_animations()
background_image = load_background()

# # Create ability-specific character images
# for char_type in ["player", "opponent"]:
#     if char_type in character_images:
#         for ability_name in ability_animations.keys():
#             ability_char_name = f"{char_type}_{ability_name}"
#             # Create a copy of the base image for each ability
#             character_images[ability_char_name] = character_images[char_type].copy()

# Animation state tracking
animation_states = {}
idle_animation_start = time.time()

# Screen flash animation state
screen_flash_state = {
    "active": False,
    "start_time": 0,
    "duration": 0.1,  # 100ms duration
    "color": (200, 200, 0),  # Yellow color to match parry theme
}

# Ability animation tracking
active_ability_animations = {}  # {character_id: {ability_name: {start_time, frames}}}


def start_ability_animation(character_id, ability_name):
    if ability_name in ability_animations:
        active_ability_animations[character_id] = {
            ability_name: {
                "start_time": time.time(),
                "frames": ability_animations[ability_name],
                "current_frame": 0,
            }
        }
        # Also update animation states for character image changes
        animation_states[f"{character_id}_{ability_name}"] = {
            "frames": ability_animations[ability_name],
            "current_frame": 0,
            "start_time": time.time(),
            "duration": 0.5,  # Half second duration
        }


def update_animations():
    """Update all active animations"""
    current_time = time.time()
    to_remove = []

    # Update ability animations
    for character_id, animations in active_ability_animations.items():
        for ability_name, anim_data in animations.items():
            frame_duration = 0.1  # 100ms per frame
            elapsed = current_time - anim_data["start_time"]
            frame_index = int(elapsed / frame_duration)

            if frame_index >= len(anim_data["frames"]):
                to_remove.append(character_id)
            else:
                anim_data["current_frame"] = frame_index

    # Update animation states
    for anim_key, anim_data in animation_states.items():
        if "start_time" in anim_data:  # Action animations
            elapsed = current_time - anim_data["start_time"]
            if elapsed >= anim_data["duration"]:
                to_remove.append(anim_key)
            else:
                frame_index = int(
                    (elapsed / anim_data["duration"]) * len(anim_data["frames"])
                )
                anim_data["current_frame"] = min(
                    frame_index, len(anim_data["frames"]) - 1
                )
        else:  # Casting animations
            if anim_data["current_frame"] >= len(anim_data["frames"]):
                anim_data["current_frame"] = 0  # Loop casting animations

    # Remove finished animations
    for key in to_remove:
        if key in active_ability_animations:
            del active_ability_animations[key]
        if key in animation_states:
            del animation_states[key]


def draw_ability_animation(screen, character_id, x, y):
    if character_id in active_ability_animations:
        for ability_name, anim_data in active_ability_animations[character_id].items():
            current_frame = anim_data["frames"][anim_data["current_frame"]]
            # Position the animation to the right of the character
            animation_x = x + CHARACTER_SIZE[0] + 20  # 20 pixels offset from character
            # Add a slight upward offset to make it look better
            animation_y = y - 20
            screen.blit(current_frame, (animation_x, animation_y))


def draw_text(surface, text, pos, font, color=(255, 255, 255)):
    rendered = font.render(text, True, color)
    surface.blit(rendered, pos)


def draw_health_bar(screen, x, y, hp, max_hp, label):
    ratio = max(hp / max_hp, 0)
    bar_color = (
        (0, 200, 0) if ratio > 0.5 else (255, 165, 0) if ratio > 0.25 else (200, 0, 0)
    )

    # Background
    pygame.draw.rect(screen, (50, 50, 50), (x, y, HP_BAR_WIDTH, HP_BAR_HEIGHT))
    # HP Bar
    pygame.draw.rect(
        screen, bar_color, (x, y, int(HP_BAR_WIDTH * ratio), HP_BAR_HEIGHT)
    )
    # Border
    pygame.draw.rect(screen, (255, 255, 255), (x, y, HP_BAR_WIDTH, HP_BAR_HEIGHT), 2)

    draw_text(screen, f"{label} HP: {hp}", (x, y - 25), small_font)


def draw_cast_bar(screen, x, y, width, height, current, total, label):
    if total == 0:
        return

    ratio = max(min((total - current) / total, 1), 0)
    pygame.draw.rect(screen, (30, 30, 30), (x, y, width, height))  # Background
    pygame.draw.rect(screen, (9, 102, 130), (x, y, int(width * ratio), height))  # Fill
    pygame.draw.rect(screen, (255, 255, 255), (x, y, width, height), 2)  # Border

    text = small_font.render(label, True, (255, 255, 255))
    screen.blit(
        text,
        (
            x + width // 2 - text.get_width() // 2,
            y + height // 2 - text.get_height() // 2,
        ),
    )


def draw_casting_info(screen, character, x, y):
    if character.casting and character.cast_ticks_remaining > 0:
        # Bar dimensions
        bar_width = 300
        bar_height = 20

        # Position the cast bar below the character
        cast_bar_y = (
            y + CHARACTER_SIZE[1] // 2 + 20
        )  # Position below the character with some padding

        draw_cast_bar(
            screen,
            x,
            cast_bar_y,
            bar_width,
            bar_height,
            character.cast_ticks_remaining,
            character.abilities[character.casting].cast_time,
            f"Casting: {character.casting}",
        )


def draw_ability_bar(screen, player, width, height):
    main_abilities = [
        "slash",
        "fireball",
        "bash",
        "vampiro",
    ]
    quick_abilities = ["parry", "kick"]

    stance_abilities = [
        "change_stance_rock",
        "change_stance_wind",
        "change_stance_water",
    ]

    spacing = 10
    total_main_width = (ABILITY_BOX_WIDTH + spacing) * len(main_abilities) - spacing
    total_stance_width = (ABILITY_BOX_WIDTH + spacing) * len(stance_abilities) - spacing

    start_x_main = 20
    start_x_stance = 20

    y_main = height - ABILITY_BOX_HEIGHT - 30
    y_stance = y_main - ABILITY_BOX_HEIGHT - 15  # 15px vertical spacing above

    font_color = (255, 255, 255)

    def draw_ability_box(ab_name, x, y, key):
        if ab_name == "quick_action":
            ab_name = STANCE_ACTION[player.stance]

        ability = player.abilities.get(ab_name)
        if not ability:
            return

        cd = getattr(ability, "remaining_cooldown", 0)
        max_cd = getattr(ability, "cooldown", 1)
        cd_ratio = cd / max_cd if max_cd else 0

        # Surface with optional dimming
        button_surface = pygame.Surface(
            (ABILITY_BOX_WIDTH, ABILITY_BOX_HEIGHT), pygame.SRCALPHA
        )
        base_color = (70, 70, 70, 255)
        button_surface.fill(base_color)

        # Dim if on global cooldown
        if player.global_cooldown > 0:
            button_surface.set_alpha(100)

        # Dim if spell ability and no gems
        if ab_name in ["fireball", "vampiro"] and player.spell_gems <= 0:
            button_surface.set_alpha(100)

        if cd > 0:
            cooldown_height = int(ABILITY_BOX_HEIGHT * cd_ratio)
            pygame.draw.rect(
                button_surface,
                (0, 0, 0, 180),
                (
                    0,
                    ABILITY_BOX_HEIGHT - cooldown_height,
                    ABILITY_BOX_WIDTH,
                    cooldown_height,
                ),
            )

        screen.blit(button_surface, (x, y))
        pygame.draw.rect(
            screen, font_color, (x, y, ABILITY_BOX_WIDTH, ABILITY_BOX_HEIGHT), 2
        )

        label = key + " - " + ab_name.replace("change_stance_", "")
        draw_text(screen, label, (x + 5, y + 5), small_font)
        draw_text(screen, f"{cd} CD", (x + 5, y + 30), small_font, (200, 200, 200))

    # Draw quick abilities
    for i, ab_name in enumerate(quick_abilities):
        x = start_x_stance + i * (ABILITY_BOX_WIDTH + spacing)
        keys = ["D", "F"]
        draw_ability_box(ab_name, x, y_main, keys[i])

    # Draw main abilities
    keys = ["Q", "W", "E", "R", "nothere", "D", "F"]
    for i, ab_name in enumerate(main_abilities):

        if ab_name != "nothere":
            x = start_x_main + i * (ABILITY_BOX_WIDTH + spacing)
            draw_ability_box(ab_name, x, y_stance, keys[i])


def draw_gcd_bar(
    screen,
    x,
    y,
    gcd_ticks,
    max_ticks=GLOBAL_COOLDOWN_TICKS,
    width=HP_BAR_WIDTH,
    height=10,
):
    # Cap ratio
    ratio = min(gcd_ticks / max_ticks, 1.0)
    color = (200, 50, 50) if ratio > 0 else (50, 200, 50)

    # Background
    pygame.draw.rect(screen, (30, 30, 30), (x, y, width, height))
    # Fill
    pygame.draw.rect(screen, color, (x, y, int(width * ratio), height))
    # Border
    pygame.draw.rect(screen, (255, 255, 255), (x, y, width, height), 1)


def draw_status_bar(
    screen, x, y, label, remaining, total, width=100, height=10, color=(100, 200, 255)
):
    ratio = max(min(remaining / total, 1.0), 0.0)
    pygame.draw.rect(screen, (30, 30, 30), (x, y, width, height))  # Background
    pygame.draw.rect(screen, color, (x, y, int(width * ratio), height))  # Fill
    pygame.draw.rect(screen, (255, 255, 255), (x, y, width, height), 1)  # Border

    label_surf = small_font.render(label, True, (255, 255, 255))
    screen.blit(label_surf, (x, y - 18))


def draw_conditions_and_stance(screen, character, x, top_y):
    # 1. Stance text
    # stance_color_map = {
    #     "rock": (0, 180, 180),
    #     "wind": (100, 220, 100),
    #     "water": (100, 150, 255),
    # }
    # stance_text = f"Stance: {character.stance}"
    # color = stance_color_map.get(character.stance, (200, 200, 200))
    # stance_surf = small_font.render(stance_text, True, color)
    # screen.blit(stance_surf, (x, top_y))

    # 2. Conditions list
    y_offset = top_y + 50
    bar_width = 110
    bar_height = 12

    for i, (cond, v) in enumerate(character.conditions.items()):
        total = v[1]
        remaining = v[0]
        bar_color = {
            "poison": (180, 60, 60),
            "regen": (60, 180, 60),
            "stun": (120, 120, 255),
        }.get(cond, (160, 160, 160))

        draw_status_bar(
            screen,
            x,
            y_offset + i * (bar_height + 18),
            cond,
            remaining,
            total,
            bar_width,
            bar_height,
            bar_color,
        )


def draw_character_figure(screen, character, x, y, is_player=True):
    # Get character image
    char_type = "player" if is_player else "opponent"
    base_image = character_images.get(char_type)

    # Check for active action animations first
    active_action = None
    active_ability = None
    for ability_name in character.abilities:
        anim_key = f"{char_type}_{ability_name}"
        if anim_key in animation_states:
            active_action = animation_states[anim_key]
            active_ability = ability_name
            break

    if base_image:
        # Calculate base position with idle animation
        current_time = time.time()
        idle_time = current_time - idle_animation_start
        idle_offset = math.sin(idle_time * 2) * 5  # 5 pixels max movement

        # Add forward movement when using an ability (opposite direction for opponent)
        forward_offset = 90 if active_action else 0  # pixels to move forward
        if not is_player:
            forward_offset = -forward_offset  # Move in opposite direction for opponent

        # Check for autoattack animation
        if "autoattack" in active_ability_animations.get(char_type, {}):
            forward_offset = (
                60 if is_player else -60
            )  # Smaller forward movement for autoattack
            active_action = active_ability_animations[char_type]["autoattack"]
            active_ability = "autoattack"

        # Determine which image to use
        current_image = base_image

        # Check for stun condition first
        if "stun" in character.conditions:
            stun_image = character_images.get(f"{char_type}_stun")
            if stun_image:
                current_image = stun_image
        # Check for casting state
        elif character.casting and character.cast_ticks_remaining > 0:
            casting_image = character_images.get(f"{char_type}_casting")
            if casting_image:
                current_image = casting_image
        # Then check for ability-specific image
        elif active_action and active_ability:
            ability_char_name = f"{char_type}_{active_ability}"
            ability_image = character_images.get(ability_char_name)
            if ability_image:
                current_image = ability_image

        # Draw the character image
        image_rect = current_image.get_rect(
            center=(x + idle_offset + forward_offset, y)
        )
        screen.blit(current_image, image_rect)

        # If there's an active action, draw the ability animation beside the character
        if active_action:
            ability_frame = active_action["frames"][active_action["current_frame"]]
            # Position the animation to the right of player, left of opponent
            animation_x = x + (
                CHARACTER_SIZE[0] // 2 if is_player else -CHARACTER_SIZE[0] - 20
            )
            # Center vertically with the character
            animation_y = y - CHARACTER_SIZE[1] // 4
            screen.blit(ability_frame, (animation_x, animation_y))
        # Handle casting animation
        elif character.casting and character.cast_ticks_remaining > 0:
            if character.casting in ability_animations:
                anim_key = f"{char_type}_casting"
                if anim_key not in animation_states:
                    animation_states[anim_key] = {
                        "frames": ability_animations[character.casting],
                        "current_frame": 0,
                    }
                anim_state = animation_states[anim_key]
                if anim_state["current_frame"] < len(anim_state["frames"]):
                    cast_frame = anim_state["frames"][anim_state["current_frame"]]
                    anim_state["current_frame"] += 1
                    if anim_state["current_frame"] >= len(anim_state["frames"]):
                        anim_state["current_frame"] = 0  # Loop casting animation
                    # Draw casting animation beside the character (opposite side for opponent)
                    animation_x = x + (
                        CHARACTER_SIZE[0] // 2 if is_player else -CHARACTER_SIZE[0] - 20
                    )
                    # Center vertically with the character
                    animation_y = y - CHARACTER_SIZE[1] // 4
                    screen.blit(cast_frame, (animation_x, animation_y))
    else:
        # Fallback to circle if no image is available
        base_color = (80, 80, 200) if is_player else (200, 80, 80)
        casting_color = (150, 150, 255) if is_player else (255, 150, 150)
        action_color = (200, 200, 100) if is_player else (200, 100, 200)

        if active_action:
            color = action_color
        elif character.casting and character.cast_ticks_remaining > 0:
            color = casting_color
        else:
            color = base_color

        # Calculate idle animation offset
        current_time = time.time()
        idle_time = current_time - idle_animation_start
        idle_offset = math.sin(idle_time * 2) * 5  # 5 pixels max movement

        # Add forward movement when using an ability (opposite direction for opponent)
        forward_offset = 90 if active_action else 0  # pixels to move forward
        if not is_player:
            forward_offset = -forward_offset  # Move in opposite direction for opponent

        pygame.draw.circle(
            screen, color, (x + idle_offset + forward_offset, y), 120
        )  # Increased from 40 to 120


def draw_countdown(screen, ticks, width, height):
    # Create a semi-transparent overlay
    overlay = pygame.Surface((width, height), pygame.SRCALPHA)
    overlay.fill((0, 0, 0, 128))  # Semi-transparent black
    screen.blit(overlay, (0, 0))

    # Draw the countdown text
    countdown_text = f"Combat starts in {ticks} ticks"
    text_surface = font.render(countdown_text, True, (255, 255, 255))
    text_rect = text_surface.get_rect(center=(width // 2, height // 2))

    # Draw a background for the text
    padding = 20
    bg_rect = text_rect.inflate(padding * 2, padding * 2)
    pygame.draw.rect(screen, (0, 0, 0, 200), bg_rect)
    pygame.draw.rect(screen, (255, 255, 255), bg_rect, 2)

    # Draw the text
    screen.blit(text_surface, text_rect)


def start_action_animation(character_id, action_name, duration=0.5):
    """Start an animation for a specific action"""
    if action_name in ability_animations:
        animation_states[f"{character_id}_{action_name}"] = {
            "frames": ability_animations[action_name],
            "current_frame": 0,
            "start_time": time.time(),
            "duration": duration,
            "ability_name": action_name,  # Store the ability name in the animation state
        }


def draw_text_bubble(screen, text, x, y, font, text_color=(0, 0, 0)):
    # Bubble size and padding
    padding = 10
    tail_height = 10
    border_radius = 15
    max_width = 220

    # Render the text
    text_surface = font.render(text, True, text_color)
    text_rect = text_surface.get_rect()
    bubble_width = min(text_rect.width + 2 * padding, max_width)
    bubble_height = text_rect.height + 2 * padding

    # Create bubble surface with alpha
    bubble_surface = pygame.Surface(
        (bubble_width, bubble_height + tail_height), pygame.SRCALPHA
    )

    # Bubble background with shadow
    shadow_offset = 4
    shadow_color = (0, 0, 0, 100)
    pygame.draw.rect(
        bubble_surface,
        shadow_color,
        (shadow_offset, shadow_offset, bubble_width, bubble_height),
        border_radius=border_radius,
    )

    # Bubble background
    bubble_color = (255, 255, 255, 240)
    pygame.draw.rect(
        bubble_surface,
        bubble_color,
        (0, 0, bubble_width, bubble_height),
        border_radius=border_radius,
    )

    # Bubble tail (triangle)
    tail_color = (255, 255, 255, 240)
    tail_points = [
        (bubble_width // 2 - 5, bubble_height),  # Left point
        (bubble_width // 2 + 5, bubble_height),  # Right point
        (
            bubble_width // 2,
            bubble_height + tail_height,
        ),  # Bottom point (points to character)
    ]
    pygame.draw.polygon(bubble_surface, tail_color, tail_points)

    # Draw text
    text_rect.center = (bubble_width // 2, bubble_height // 2)
    bubble_surface.blit(text_surface, text_rect)

    # Blit to main screen (above character, centered)
    screen.blit(
        bubble_surface, (x - bubble_width // 2, y - bubble_height - tail_height - 10)
    )


def draw_spell_gems(screen, character, x, y):
    """Draw spell gems for the character"""
    gem_size = 20
    gem_spacing = 5
    gem_color = (102, 217, 255)  # Blue color for spell gems
    empty_gem_color = (50, 50, 100)  # Darker blue for empty gems

    # Draw gem container
    container_width = (gem_size + gem_spacing) * 3
    pygame.draw.rect(screen, (30, 30, 60), (x, y, container_width, gem_size), 2)

    # Draw individual gems
    for i in range(3):
        gem_x = x + i * (gem_size + gem_spacing)
        color = gem_color if i < character.spell_gems else empty_gem_color
        pygame.draw.circle(
            screen, color, (gem_x + gem_size // 2, y + gem_size // 2), gem_size // 2 - 2
        )


def draw_parry_counter(screen, character, x, y, width=200, height=10):
    """Draw the parry counter bar below the ability crystals"""
    # Background
    pygame.draw.rect(screen, (50, 50, 50), (x, y, width, height))

    # Calculate fill width based on parry counter
    fill_width = int((character.parry_counter / character.parry_counter_max) * width)

    # Fill bar
    pygame.draw.rect(
        screen, (200, 200, 0), (x, y, fill_width, height)
    )  # Yellow color for parry

    # Border
    pygame.draw.rect(screen, (255, 255, 255), (x, y, width, height), 2)

    # Draw segments
    segment_width = width / 4  # 4 segments for 8 steps (2 steps per segment)
    for i in range(1, 4):
        segment_x = x + i * segment_width
        pygame.draw.line(
            screen, (100, 100, 100), (segment_x, y), (segment_x, y + height), 1
        )

    # Draw label
    text = small_font.render("Parry Counter", True, (255, 255, 255))
    screen.blit(text, (x, y - 20))


def trigger_screen_flash():
    """Trigger a screen flash animation"""
    global screen_flash_state
    screen_flash_state["active"] = True
    screen_flash_state["start_time"] = time.time()


def draw_screen_flash(screen):
    """Draw the screen flash overlay if active"""
    if not screen_flash_state["active"]:
        return

    current_time = time.time()
    elapsed = current_time - screen_flash_state["start_time"]

    if elapsed >= screen_flash_state["duration"]:
        screen_flash_state["active"] = False
        return

    # Create a semi-transparent overlay
    overlay = pygame.Surface(screen.get_size(), pygame.SRCALPHA)
    # Calculate alpha based on elapsed time (fade out)
    alpha = int(255 * (1 - elapsed / screen_flash_state["duration"]))
    color = (*screen_flash_state["color"], alpha)
    overlay.fill(color)
    screen.blit(overlay, (0, 0))


def draw_decision_counter(screen, character, x, y, width=200, height=10):
    """Draw the decision counter bar below the opponent character"""
    # Background
    pygame.draw.rect(screen, (50, 50, 50), (x, y, width, height))

    # Calculate fill width based on decision counter
    fill_width = int((character.n_decisions / (50 * 2)) * width)

    # Fill bar
    pygame.draw.rect(
        screen, (100, 100, 255), (x, y, fill_width, height)
    )  # Blue color for decisions

    # Border
    pygame.draw.rect(screen, (255, 255, 255), (x, y, width, height), 2)

    # Draw segments
    segment_width = width / 4  # 4 segments
    for i in range(1, 4):
        segment_x = x + i * segment_width
        pygame.draw.line(
            screen, (100, 100, 100), (segment_x, y), (segment_x, y + height), 1
        )

    # Draw label
    text = small_font.render("Incoming Slash", True, (255, 255, 255))
    screen.blit(text, (x, y - 20))


def draw_ui(screen, player, opponent, countdown_ticks=None):
    width, height = screen.get_size()

    # Draw background if available
    if background_image:
        # Scale background to fit screen
        scaled_background = pygame.transform.scale(background_image, (width, height))
        screen.blit(scaled_background, (0, 0))
    else:
        # Fallback to black background
        screen.fill((0, 0, 0))

    # Draw HP Bars
    player_hp_x = 20
    opponent_hp_x = width - HP_BAR_WIDTH - 20

    draw_health_bar(screen, player_hp_x, TOP_PADDING, player.hp, MAX_HP, "Player")
    draw_health_bar(
        screen, opponent_hp_x, TOP_PADDING, opponent.hp, MAX_HP_OPPONENT, "Enemy"
    )

    # Draw GCD Bars
    draw_gcd_bar(
        screen, player_hp_x, TOP_PADDING + HP_BAR_HEIGHT + 5, player.global_cooldown
    )
    draw_gcd_bar(
        screen, opponent_hp_x, TOP_PADDING + HP_BAR_HEIGHT + 5, opponent.global_cooldown
    )

    # Draw Spell Gems
    draw_spell_gems(screen, player, player_hp_x, TOP_PADDING + HP_BAR_HEIGHT + 30)
    draw_spell_gems(screen, opponent, opponent_hp_x, TOP_PADDING + HP_BAR_HEIGHT + 30)

    # Draw parry counter bar below ability crystals
    draw_parry_counter(screen, player, player_hp_x, 140)

    # Stance and Conditions
    draw_conditions_and_stance(screen, player, player_hp_x, 120)
    draw_conditions_and_stance(screen, opponent, opponent_hp_x, 120)

    # Draw ability UI
    draw_ability_bar(screen, player, width, height)

    player_x = width // 2 - 200
    opponent_x = width // 2 + 200
    figure_y = height // 2 - 10

    draw_character_figure(screen, player, player_x, figure_y, is_player=True)
    draw_character_figure(screen, opponent, opponent_x, figure_y, is_player=False)

    # Draw decision counter below opponent
    draw_decision_counter(screen, opponent, opponent_x - 100, figure_y + 200)

    # Draw casting info
    draw_casting_info(screen, player, player_x - 150, figure_y + 60)
    draw_casting_info(screen, opponent, opponent_x - 150, figure_y + 60)

    if opponent.next_action != "do_nothing":
        draw_text_bubble(
            screen,
            f"{opponent.next_action}...",
            opponent_x - 100,
            figure_y - 200,
            small_font,
        )

    # Draw countdown timer if active
    if countdown_ticks is not None:
        draw_countdown(screen, countdown_ticks, width, height)

    # Draw screen flash last so it appears on top of everything
    draw_screen_flash(screen)


def draw_game_over(screen, winner):
    width, height = screen.get_size()

    # Create a semi-transparent overlay
    overlay = pygame.Surface((width, height), pygame.SRCALPHA)
    overlay.fill((0, 0, 0, 180))  # Semi-transparent black
    screen.blit(overlay, (0, 0))

    # Draw the winner/loser text
    result_text = "You Win!" if winner == "Player" else "You Lose!"
    result_surface = font.render(result_text, True, (255, 255, 255))
    result_rect = result_surface.get_rect(center=(width // 2, height // 2))

    # Draw a background for the result text
    padding = 20
    bg_rect = result_rect.inflate(padding * 2, padding * 2)
    pygame.draw.rect(screen, (0, 0, 0, 200), bg_rect)
    pygame.draw.rect(screen, (255, 255, 255), bg_rect, 2)
    screen.blit(result_surface, result_rect)

    # Draw restart instruction
    restart_text = "Press SPACE to restart"
    restart_surface = small_font.render(restart_text, True, (200, 200, 200))
    restart_rect = restart_surface.get_rect(center=(width // 2, height // 2 + 100))
    screen.blit(restart_surface, restart_rect)
