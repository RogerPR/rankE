import pygame
import time
from character import Character
from constants import TICK_DURATION, MAX_HP, MAX_HP_OPPONENT
from ui import (
    draw_stance_selection,
    draw_weapon_selection,
    draw_armor_selection,
    draw_start_screen,
)
from ui_aux import (
    animation_states,
    ability_animations,
    start_ability_animation,
    update_animations,
    start_action_animation,
    draw_ui,
    draw_game_over,
)
from floating_text import FloatingTextManager


def game_loop():
    pygame.init()
    screen = pygame.display.set_mode((1200, 900))
    clock = pygame.time.Clock()

    def reset_game():
        nonlocal player, opponent, tick_count, countdown_ticks, combat_started
        nonlocal stance_selected, selected_stance, weapon_selected, selected_weapon
        nonlocal armor_selected, selected_armor, game_over, winner, game_started
        player = Character("Player", MAX_HP)
        opponent = Character("Enemy", MAX_HP_OPPONENT)
        tick_count = 0
        countdown_ticks = 30
        combat_started = False
        stance_selected = False
        selected_stance = None
        weapon_selected = False
        selected_weapon = None
        armor_selected = False
        selected_armor = None
        game_over = False
        winner = None
        game_started = False

    player = Character("Player", MAX_HP)
    opponent = Character("Enemy", MAX_HP_OPPONENT)
    floating_text_manager = FloatingTextManager()

    tick_count = 0
    running = True
    countdown_ticks = 30
    combat_started = False
    stance_selected = False
    selected_stance = None
    weapon_selected = False
    selected_weapon = None
    armor_selected = False
    selected_armor = None
    game_over = False
    winner = None
    game_started = False

    while running:
        tick_start = time.time()

        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                running = False
            elif event.type == pygame.KEYDOWN:
                if not game_started:
                    if event.key == pygame.K_SPACE:
                        game_started = True
                elif not stance_selected:
                    if event.key == pygame.K_1:
                        selected_stance = "rock"
                        stance_selected = True
                        player.selected_stance = "rock"
                    elif event.key == pygame.K_2:
                        selected_stance = "wind"
                        stance_selected = True
                        player.selected_stance = "wind"
                    elif event.key == pygame.K_3:
                        selected_stance = "water"
                        stance_selected = True
                        player.selected_stance = "water"
                elif not weapon_selected:
                    if event.key == pygame.K_1:
                        selected_weapon = "sword"
                        weapon_selected = True
                        player.selected_weapon = "sword"
                    elif event.key == pygame.K_2:
                        selected_weapon = "dagger"
                        weapon_selected = True
                        player.selected_weapon = "dagger"
                    elif event.key == pygame.K_3:
                        selected_weapon = "wand"
                        weapon_selected = True
                        player.selected_weapon = "wand"
                elif not armor_selected:
                    if event.key == pygame.K_1:
                        selected_armor = "light"
                        armor_selected = True
                        player.selected_armor = "light"
                    elif event.key == pygame.K_2:
                        selected_armor = "medium"
                        armor_selected = True
                        player.selected_armor = "medium"
                    elif event.key == pygame.K_3:
                        selected_armor = "heavy"
                        armor_selected = True
                        player.selected_armor = "heavy"
                elif game_over and event.key == pygame.K_SPACE:
                    reset_game()

        if not game_started:
            # Show start screen
            draw_start_screen(screen)
            pygame.display.flip()
            continue
        elif not stance_selected:
            # Show stance selection screen
            screen.fill((0, 0, 0))
            draw_stance_selection(screen)
            pygame.display.flip()
            continue
        elif not weapon_selected:
            # Show weapon selection screen
            screen.fill((0, 0, 0))
            draw_weapon_selection(screen)
            pygame.display.flip()
            continue
        elif not armor_selected:
            # Show armor selection screen
            screen.fill((0, 0, 0))
            draw_armor_selection(screen)
            pygame.display.flip()
            continue

        if not combat_started:
            countdown_ticks -= 1
            if countdown_ticks <= 0:
                combat_started = True
                # Set the selected stance
                player.update_stance(selected_stance)
                player.update_choices()

        elif not game_over:
            player.tick()
            opponent.tick()

            # Handle autoattack
            autoattack_player = player.autoattack()
            autoattack_opponent = opponent.autoattack()

            if autoattack_player:
                start_ability_animation("player", "autoattack")
            if autoattack_opponent:
                start_ability_animation("opponent", "autoattack")

            opponent.receive_action(autoattack_player)
            player.receive_action(autoattack_opponent)

            keys = pygame.key.get_pressed()

            ability = pygame_key_to_ability_name(keys)
            action_player = player.act(ability)

            ability_opponent = opponent.decide_what_to_do(player)
            action_opponent = opponent.act(ability_opponent)

            # Only start animations if abilities were successfully used
            if action_player and ability != "do_nothing":
                start_ability_animation("player", ability)
                # Add floating text for player ability
                floating_text_manager.add_text(
                    ability.upper(),
                    screen.get_width() // 2 - 300,  # Player x position
                    screen.get_height() // 2 - 250,  # Moved higher up
                    (100, 255, 100),  # Green color for player abilities
                )
            if action_opponent and ability_opponent != "do_nothing":
                start_ability_animation("opponent", ability_opponent)
                # Add floating text for opponent ability
                floating_text_manager.add_text(
                    ability_opponent.upper(),
                    screen.get_width() // 2 + 300,  # Opponent x position
                    screen.get_height() // 2 - 250,  # Moved higher up
                    (255, 100, 100),  # Red color for opponent abilities
                )

            # Handle damage floating text
            if action_player and action_player.get("damage", 0) > 0:
                floating_text_manager.add_text(
                    f"-{action_player['damage']}",
                    screen.get_width() // 2 + 300,  # Opponent x position
                    screen.get_height() // 2 - 200,  # Moved higher up
                    (255, 50, 50),  # Red color for damage
                )
            if action_opponent and action_opponent.get("damage", 0) > 0:
                floating_text_manager.add_text(
                    f"-{action_opponent['damage']}",
                    screen.get_width() // 2 - 300,  # Player x position
                    screen.get_height() // 2 - 200,  # Moved higher up
                    (255, 50, 50),  # Red color for damage
                )

            player.receive_action(action_player, self_action=True)
            player.receive_action(action_opponent)

            opponent.receive_action(action_opponent, self_action=True)
            opponent.receive_action(action_player)

            # Check for game over condition
            if player.hp <= 0 or opponent.hp <= 0:
                game_over = True
                winner = "Enemy" if player.hp <= 0 else "Player"

        # Update animations and floating text
        update_animations()
        floating_text_manager.update()

        # Draw the game state
        screen.fill((0, 0, 0))  # Clear screen
        draw_ui(
            screen, player, opponent, countdown_ticks if not combat_started else None
        )
        floating_text_manager.draw(screen)  # Draw floating text

        # Draw game over screen if game is over
        if game_over:
            draw_game_over(screen, winner)

        pygame.display.flip()

        # Maintain tick duration
        tick_duration = TICK_DURATION - (time.time() - tick_start)
        if tick_duration > 0:
            time.sleep(tick_duration)

        tick_count += 1
        clock.tick(60)

    pygame.quit()


def pygame_key_to_ability_name(keys):
    if keys[pygame.K_q]:
        return "slash"
    elif keys[pygame.K_w]:
        return "fireball"
    elif keys[pygame.K_e]:
        return "bash"
    elif keys[pygame.K_r]:
        return "vampiro"
    elif keys[pygame.K_d]:
        return "parry"
    elif keys[pygame.K_f]:
        return "kick"
    # elif keys[pygame.K_1]:
    #     return "change_stance_rock"
    # elif keys[pygame.K_2]:
    #     return "change_stance_wind"
    # elif keys[pygame.K_3]:
    #     return "change_stance_water"
    else:
        return "do_nothing"  # No input, player skips or defaults to "wait"
