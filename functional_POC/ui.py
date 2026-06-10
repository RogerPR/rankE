from constants import MAX_HP, MAX_HP_OPPONENT
from ui_aux import *

HP_BAR_WIDTH = 200
HP_BAR_HEIGHT = 20
TOP_PADDING = 40


def draw_stance_selection(screen):
    width, height = screen.get_size()

    # Title
    title = font.render("Choose Your Stance", True, (255, 255, 255))
    title_rect = title.get_rect(center=(width // 2, height // 4))
    screen.blit(title, title_rect)

    # Stance options
    stances = [
        ("Rock Stance", "1", (0, 180, 180)),
        ("Wind Stance", "2", (100, 220, 100)),
        ("Water Stance", "3", (100, 150, 255)),
    ]

    box_width = 300
    box_height = 200
    spacing = 50
    total_width = (box_width + spacing) * len(stances) - spacing
    start_x = (width - total_width) // 2
    y = height // 2

    text = [
        "Increase Autoatac (AA) dmg",
        "Parry has half cooldown, -10% hp",
        "7 spell gems, -20% hp",
    ]
    for i, (name, key, color) in enumerate(stances):
        x = start_x + i * (box_width + spacing)

        # Draw box
        box_rect = pygame.Rect(x, y, box_width, box_height)
        pygame.draw.rect(screen, (50, 50, 50), box_rect)
        pygame.draw.rect(screen, color, box_rect, 3)

        # Draw stance name
        name_surf = font.render(name, True, color)
        name_rect = name_surf.get_rect(center=(x + box_width // 2, y + 50))
        screen.blit(name_surf, name_rect)

        # Draw key prompt
        key_surf = font.render(f"Press {key}", True, (255, 255, 255))
        key_rect = key_surf.get_rect(center=(x + box_width // 2, y + box_height - 50))
        screen.blit(key_surf, key_rect)

        # Draw stance description
        desc = small_font.render(
            text[i],
            True,
            (200, 200, 200),
        )
        desc_rect = desc.get_rect(center=(x + box_width // 2, y + 100))
        screen.blit(desc, desc_rect)


def draw_weapon_selection(screen):
    width, height = screen.get_size()

    # Title
    title = font.render("Choose Your Weapon", True, (255, 255, 255))
    title_rect = title.get_rect(center=(width // 2, height // 4))
    screen.blit(title, title_rect)

    # Weapon options
    weapons = [
        ("Sword", "1", (180, 180, 0)),
        ("Dagger", "2", (180, 0, 0)),
        ("Wand", "3", (0, 180, 0)),
    ]

    box_width = 300
    box_height = 200
    spacing = 50
    total_width = (box_width + spacing) * len(weapons) - spacing
    start_x = (width - total_width) // 2
    y = height // 2

    text = ["-", "+20% AA speed, -10% AA damage", "-20% cast time"]
    for i, (name, key, color) in enumerate(weapons):
        x = start_x + i * (box_width + spacing)

        # Draw box
        box_rect = pygame.Rect(x, y, box_width, box_height)
        pygame.draw.rect(screen, (50, 50, 50), box_rect)
        pygame.draw.rect(screen, color, box_rect, 3)

        # Draw weapon name
        name_surf = font.render(name, True, color)
        name_rect = name_surf.get_rect(center=(x + box_width // 2, y + 50))
        screen.blit(name_surf, name_rect)

        # Draw key prompt
        key_surf = font.render(f"Press {key}", True, (255, 255, 255))
        key_rect = key_surf.get_rect(center=(x + box_width // 2, y + box_height - 50))
        screen.blit(key_surf, key_rect)

        # Draw weapon description
        desc = small_font.render(
            text[i],
            True,
            (200, 200, 200),
        )
        desc_rect = desc.get_rect(center=(x + box_width // 2, y + 100))
        screen.blit(desc, desc_rect)


def draw_armor_selection(screen):
    width, height = screen.get_size()

    # Title
    title = font.render("Choose Your Armor", True, (255, 255, 255))
    title_rect = title.get_rect(center=(width // 2, height // 4))
    screen.blit(title, title_rect)

    # Armor options
    armors = [
        ("Light", "1", (0, 180, 180)),
        ("Medium", "2", (180, 180, 0)),
        ("Heavy", "3", (180, 0, 0)),
    ]

    box_width = 300
    box_height = 200
    spacing = 50
    total_width = (box_width + spacing) * len(armors) - spacing
    start_x = (width - total_width) // 2
    y = height // 2

    text = ["-10% cooldown, +10%dmg", "-", "+10% cooldown, -10%dmg"]
    for i, (name, key, color) in enumerate(armors):
        x = start_x + i * (box_width + spacing)

        # Draw box
        box_rect = pygame.Rect(x, y, box_width, box_height)
        pygame.draw.rect(screen, (50, 50, 50), box_rect)
        pygame.draw.rect(screen, color, box_rect, 3)

        # Draw armor name
        name_surf = font.render(name, True, color)
        name_rect = name_surf.get_rect(center=(x + box_width // 2, y + 50))
        screen.blit(name_surf, name_rect)

        # Draw key prompt
        key_surf = font.render(f"Press {key}", True, (255, 255, 255))
        key_rect = key_surf.get_rect(center=(x + box_width // 2, y + box_height - 50))
        screen.blit(key_surf, key_rect)

        # Draw armor description
        desc = small_font.render(
            text[i],
            True,
            (200, 200, 200),
        )
        desc_rect = desc.get_rect(center=(x + box_width // 2, y + 100))
        screen.blit(desc, desc_rect)


def draw_start_screen(screen):
    # Load and scale the background image to fit the screen
    try:
        background = pygame.image.load("assets/image.png")
        background = pygame.transform.scale(
            background, (screen.get_width(), screen.get_height())
        )
        screen.blit(background, (0, 0))
    except:
        # If image loading fails, use a black background
        screen.fill((0, 0, 0))

    # Create font for the text
    font = pygame.font.Font(None, 74)
    text = font.render("Press SPACE to Start", True, (255, 255, 255))
    text_rect = text.get_rect(
        center=(screen.get_width() // 2, screen.get_height() - 100)
    )

    # Draw the text
    screen.blit(text, text_rect)
